using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ScrumPoker.Data.Models;

namespace ScrumPoker.Data.Services;

public class SessionService : ISessionService
{
    private IMongoCollection<Session> _sessions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionService(IOptions<ScrumPokerDatabaseSettings> dbSettings, IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        var client = new MongoClient(dbSettings.Value.ConnectionString);
        var mongoDb = client.GetDatabase(dbSettings.Value.DatabaseName);
        _sessions = mongoDb.GetCollection<Session>(dbSettings.Value.SessionsCollectionName);
    }

    public async Task<Session> CreateSessionAsync(string creatorId, string sessionName)
    {
        var newSession = new Session
        {
            Id = GenerateRandomId(),
            DisplayName = sessionName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Participants = [],
            CreatorId = GetOrCreateUserId(),
        };
        await _sessions.InsertOneAsync(newSession);
        return newSession;
    }

    public async Task<ErrorOr<string>> JoinSessionAsync(long sessionId, CancellationToken cancellationToken)
    {
        var session = await GetSessionByIdAsync(sessionId, cancellationToken);
        if (session.IsError)
            return session.Errors.FirstOrDefault();

        var userId = GetOrCreateUserId();
        session.Value.Participants.Add(new Participant { Id = userId });
        await _sessions.ReplaceOneAsync(x => x.Id == sessionId, session.Value, cancellationToken: cancellationToken);
        return userId;
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
        catch (Exception ex)
        {
            return Errors.Session.SessionNotFound;
        }
    }

    public async ValueTask<ErrorOr<Success>> UpdateUserNameAsync(long sessionId, string userName)
    {
        var userId = GetUserIdFromCookie();
        if (userId == null)
            return Errors.Session.UserNotCreated;

        var sessionResult = await GetSessionByIdAsync(sessionId, default);
        if (sessionResult.IsError)
            return Errors.Session.SessionNotFound;
        
        var session = sessionResult.Value;
        var participant = session.Participants.First(x => x.Id == userId);
        participant.DisplayName = userName;

        await _sessions.ReplaceOneAsync(x => x.Id == sessionId, session);
        return Result.Success;
    }

    #region PrivateMethods
    private string GetOrCreateUserId()
    {
        var userId = GetUserIdFromCookie();
        if (userId != null)
            return userId;

        return ObjectId.GenerateNewId().ToString();
    }

    private string? GetUserIdFromCookie()
    {
        var context = _httpContextAccessor.HttpContext ??
          throw new InvalidOperationException("HttpContext is not available.");
        if (context.Request.Cookies.TryGetValue("UserId", out var userId))
            return userId;

        return null;
    }

    private int GenerateRandomId()
    {
        long timeSeed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomPart = Random.Shared.Next(0, 100000);
        int combined = (int)((timeSeed + randomPart) % 90000000) + 10000000;
        return combined;
    }

   
    #endregion
}
