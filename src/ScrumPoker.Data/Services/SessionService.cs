using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrumPoker.Data.Dto;
using ScrumPoker.Data.Dto.Backlogs;
using ScrumPoker.Data.Hubs;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public class SessionService : ISessionService
{
    #region Fields
    private readonly IMongoCollection<Session> _sessions;
    private readonly IMongoCollection<Participant> _participants;
    private readonly IMongoCollection<Backlog> _backlogs;
    private readonly IMongoCollection<Estimate> _estimates;
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    #endregion

    #region Ctor
    public SessionService(
        IMongoClient mongoClient,
        IHttpContextAccessor httpContextAccessor,
        IHubContext<SessionHub> hubContext,
        IMongoClient client)
    {
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;

        var mongoDb = client.GetDatabase("ScrumPoker");
        _sessions = mongoDb.GetCollection<Session>("sessions");
        _participants = mongoDb.GetCollection<Participant>("participants");
        _backlogs = mongoDb.GetCollection<Backlog>("backlogs");
        _estimates = mongoDb.GetCollection<Estimate>("estimates");
    }

    #endregion

    #region CreateSession
    public async Task<ErrorOr<SessionDto>> CreateSessionAsync(string sessionName)
    {
        var userId = GetOrCreateUserId();
        var newSession = Session.Create(sessionName, userId);
        var newBacklog = Backlog.Create(newSession.Id.Value, userId);

        try
        {
            await _sessions.InsertOneAsync(newSession);
            await _backlogs.InsertOneAsync(newBacklog);

            await _sessions.UpdateOneAsync(x => x.Id == newSession.Id,
                Builders<Session>.Update.Set(x => x.ActiveTaskId, newBacklog.Id),
                new UpdateOptions { IsUpsert = true });

            newSession.ActiveTaskId = newBacklog.Id;
            return newSession.ToDto([], [newBacklog.ToDto()]); //TODO: should improved
        }
        catch (Exception)
        {
            return Errors.Session.CannotCreateSession;
        }
    }
    #endregion

    #region JoinSession
    public async Task<ErrorOr<ParticipantDto>> JoinSessionAsync(long sessionId, string displayName, CancellationToken cancellationToken)
    {
        var session = await GetSessionByIdAsync(sessionId, cancellationToken);
        if (session.IsError)
            return session.Errors.FirstOrDefault();

        var userId = GetOrCreateUserId();
        var participant = new Participant
        {
            Id = ObjectId.Parse(userId),
            SessionId = session.Value.Id,
            DisplayName = displayName
        };

        try
        {
            await _participants.InsertOneAsync(participant, cancellationToken: cancellationToken);
            await SendSocketEventToClientsAsync("ParticipantJoined", sessionId, participant.ToDto(), cancellationToken);
            return participant.ToDto();
        }
        catch (Exception)
        {
            return Errors.Session.CannotJoinToSession;
        }
    }


    #endregion

    #region GetSessionById
    public async Task<ErrorOr<SessionDto>> GetSessionByIdAsync(long sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.Find(x => x.Id == sessionId).FirstOrDefaultAsync(cancellationToken);
            if (session is null)
                return Errors.Session.SessionNotFound;
            if (session.IsExpired)
                return Errors.Session.SessionIsExpired;

            //TODO: wire up estimates
            var participants = (await _participants.Find(x => x.SessionId == sessionId)
                                .ToListAsync(cancellationToken))
                                .Select(x => x.ToDto())
                                .ToList();

            var backlogs = (await _backlogs.Find(x => x.SessionId == sessionId)
                                .ToListAsync(cancellationToken))
                                .Select(x => x.ToDto())
                                .ToList();

            return session.ToDto(participants, backlogs);
        }
        catch (Exception)
        {
            return Errors.Session.SessionNotFound;
        }
    }
    #endregion


    public async Task<ErrorOr<Success>> RemoveParticipantFromSessionAsync(long sessionId, string participantId, CancellationToken cancellationToken)
    {
        var session = await _sessions.Find(x => x.Id == sessionId).FirstOrDefaultAsync();
        if (session is null)
            return Errors.Session.SessionNotFound;

        var filter = Builders<Participant>.Filter.Eq(x => x.Id, ObjectId.Parse(participantId));
        var result = await _participants.DeleteOneAsync(filter, cancellationToken);
        if (result.DeletedCount > 0)
        {
            await SendSocketEventToClientsAsync("ParticipantLeft", sessionId, participantId, cancellationToken);   
            return Result.Success;
        }

        return Errors.Session.CannotRemoveParticipant;
    }

    public async Task<ErrorOr<Success>> EstimateTaskAsync(long sessionId, string backlogId, string participantId, int value)
    {
        var session = await _sessions.Find(x => x.Id == sessionId).FirstOrDefaultAsync();
        if (session is null)
            return Errors.Session.SessionNotFound;

        var backlog = _backlogs.Find(x=>x.Id == ObjectId.Parse(backlogId) && x.SessionId == sessionId).FirstOrDefault();
        if (backlog is null)
            return Errors.Session.BacklogNotFound;

        var filter = Builders<Estimate>.Filter.And(
            Builders<Estimate>.Filter.Eq(x => x.BacklogId, ObjectId.Parse(backlogId)),
            Builders<Estimate>.Filter.Eq(x => x.ParticipantId, ObjectId.Parse(participantId))
        );

        var update = Builders<Estimate>.Update.Set(x => x.Value, value);

        await _estimates.UpdateOneAsync(
            filter,
            update,
            new UpdateOptions { IsUpsert = true });

        await SendSocketEventToClientsAsync("TaskEstimated", sessionId, new EstimateDto
        {
            BacklogId = backlogId,
            ParticipantId = participantId,
        }, CancellationToken.None);

        return Result.Success;
    }


    #region PrivateMethods
    private string GetOrCreateUserId()
        => GetUserIdFromCookie() ?? ObjectId.GenerateNewId().ToString();


    private string? GetUserIdFromCookie()
    {
        var context = _httpContextAccessor.HttpContext ??
          throw new InvalidOperationException("HttpContext is not available.");
        if (context.Request.Cookies.TryGetValue("UserId", out var userId))
            return userId;

        return null;
    }

    private async Task SendSocketEventToClientsAsync(string eventName, long sessionId, object data, CancellationToken cancellationToken)
    {
        await _hubContext.Clients.Group(sessionId.ToString()).SendAsync(eventName, data, cancellationToken);
    }

    public async Task<ErrorOr<Success>> RevealResultsAsync(long sessionId, CancellationToken cancellationToken)
    {
        //TODO: check for admin role of caller
        var session = _sessions.Find(x => x.Id == sessionId).FirstOrDefault();
        if (session is null)
            return Errors.Session.SessionNotFound;

        var estimates = await _estimates.Find(x => x.BacklogId == session.ActiveTaskId).ToListAsync();
        //TODO: make active backlog revealed
        var backlog = _backlogs.Find(x => x.Id == session.ActiveTaskId).FirstOrDefault();

        await _backlogs.UpdateOneAsync(x => x.Id == backlog.Id,
                Builders<Backlog>.Update.Set(x => x.IsRevealed, true), cancellationToken: cancellationToken);

        var resultSummary = new BacklogEstimateSummaryDto
        {
            Estimates = estimates.ToList().Select(e => e.ToDto()).ToList(),
            BacklogId = session.ActiveTaskId.ToString(),
            Average = estimates.Average(e => e.Value),
            Mood = estimates.GroupBy(e => e.Value)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? 0,
        };

        await SendSocketEventToClientsAsync("EstimationRevealed", sessionId, resultSummary, default);
        return Result.Success;
    }
    #endregion
}
