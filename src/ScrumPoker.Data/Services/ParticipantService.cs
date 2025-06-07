using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ScrumPoker.Data.Models;
using ErrorOr;
using MongoDB.Bson;

namespace ScrumPoker.Data.Services;

public class ParticipantService
{
    private readonly IMongoCollection<Participant> _participants;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ParticipantService(
        IOptions<ScrumPokerDatabaseSettings> dbSettings,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;

        var client = new MongoClient(dbSettings.Value.ConnectionString);
        var mongoDb = client.GetDatabase(dbSettings.Value.DatabaseName);
        _participants = mongoDb.GetCollection<Participant>("participants");
    }

    public async Task<ErrorOr<Participant>> Create(long sessionId, string displayName)
    {
        var id = GetOrCreateUserId();
        var existingUser = _participants.Find(x => x.Id == id).FirstOrDefaultAsync();
        if (existingUser is not null)
        {
            return Errors.Participant.ParticipantExists;
        }

        try
        {
            var newParticipant = Participant.Create(id, sessionId, displayName);
            await _participants.InsertOneAsync(newParticipant);
            return newParticipant;
        }
        catch (Exception ex)
        {
            //TODO: log
            return Errors.Participant.CannotCreate;
        }
    }


    #region PrivateMethods
    private ObjectId GetOrCreateUserId()
        => GetUserIdFromCookie() ?? ObjectId.GenerateNewId();

    private ObjectId? GetUserIdFromCookie()
    {
        var context = _httpContextAccessor.HttpContext ??
          throw new InvalidOperationException("HttpContext is not available.");
        if (context.Request.Cookies.TryGetValue("UserId", out var userId))
            return ObjectId.Parse(userId);

        return null;
    }
    #endregion
}
