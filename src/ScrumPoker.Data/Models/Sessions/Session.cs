using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using ScrumPoker.Data.Dto;

namespace ScrumPoker.Data.Models;

public class Session
{
    #region Fields
    [BsonId]
    public long? Id { get; set; }

    public string? Title { get; set; }

    public required string CreatorId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; }

    public ObjectId ActiveTaskId { get; set; }

    public bool IsExpired => CreatedAtUtc < DateTime.UtcNow.AddHours(-3);
    #endregion

    #region Constructor
    internal static Session Create(string sessionName, string userId)
    {
        var defaultBacklog = Backlog.Create();
        return new Session
        {
            Id = GenerateRandomId(),
            Title = sessionName,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true,
            CreatorId = userId,
            ActiveTaskId = defaultBacklog.Id
        };
    }

    private static int GenerateRandomId()
    {
        long timeSeed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomPart = Random.Shared.Next(0, 100000);
        int combined = (int)((timeSeed + randomPart) % 900000000) + 100000000;
        return combined;
    }
    #endregion

    #region Mappings
    public SessionDto ToDto(List<ParticipantDto>? participants = null, List<BacklogDto>? backlogs = null)
    {
        return new SessionDto
        {
            Id = Id.Value,
            Title = Title,
            CreatorId = CreatorId,
            IsExpired = IsExpired,
            Participants = participants ?? [],
            Backlogs = backlogs ?? []
        };
    }
   
    #endregion
}
