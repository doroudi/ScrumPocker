using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrumPoker.Data.Hubs;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public class SessionService : ISessionService
{
    private readonly IMongoCollection<Session> _sessions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionService(
        IOptions<ScrumPokerDatabaseSettings> dbSettings,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;

        var client = new MongoClient(dbSettings.Value.ConnectionString);
        var mongoDb = client.GetDatabase(dbSettings.Value.DatabaseName);
        _sessions = mongoDb.GetCollection<Session>(dbSettings.Value.SessionsCollectionName);
    }

    public async Task<ErrorOr<Session>> CreateSessionAsync(string creatorId, string sessionName)
    {
        var userId = GetOrCreateUserId();
        var newSession = Session.Create(sessionName, userId);

        try
        {
            await _sessions.InsertOneAsync(newSession);
            return newSession;
        }
        catch (Exception)
        {
            return Errors.Session.CannotCreateSession;
        }
    }

    public async Task<ErrorOr<Participant>> JoinSessionAsync(long sessionId, string displayName, CancellationToken cancellationToken)
    {
        var session = await GetSessionByIdAsync(sessionId, cancellationToken);
        if (session.IsError)
            return session.Errors.FirstOrDefault();

        var userId = GetOrCreateUserId();
        var participant = new Participant
        {
            Id = userId,
            DisplayName = displayName
        };

        try
        {
            var update = Builders<Session>.Update.AddToSet(x => x.Participants, participant);
            var result = await _sessions.UpdateOneAsync(x => x.Id == sessionId, update, cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
                return Errors.Session.SessionNotFound;

            return participant;
        }
        catch (Exception)
        {
            return Errors.Session.CannotJoinToSession;
        }
    }

    public async Task<ErrorOr<Session>> GetSessionByIdAsync(long sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _sessions.Find(x => x.Id == sessionId).FirstOrDefaultAsync(cancellationToken);
            if (session is null)
                return Errors.Session.SessionNotFound;
            if (session.IsExpired)
                return Errors.Session.SessionIsExpired;

            return session;
        }
        catch (Exception)
        {
            return Errors.Session.SessionNotFound;
        }
    }


    public async Task<ErrorOr<Success>> RemoveParticipantFromSessionAsync(string sessionId, string participantId)
    {
        var id = long.Parse(participantId);
        var session = await _sessions.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (session is null)
            return Errors.Session.SessionNotFound;

        var filter = Builders<Session>.Filter.Eq(x => x.Id, id);
        var update = Builders<Session>.Update.PullFilter(x => x.Participants, p => p.Id == participantId);
        var result = await _sessions.UpdateOneAsync(filter, update);
        if (result.ModifiedCount > 0)
            return Result.Success;

        return Errors.Session.CannotRemoveParticipant;
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

    
    #endregion
}
