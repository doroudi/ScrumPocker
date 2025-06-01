using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ScrumPoker.Data.Models;

public class Session
{
    #region Fields
    [BsonId]
    public long? Id { get; set; }

    [BsonElement("displayName")]
    public string? DisplayName { get; set; }

    [BsonElement("creatorId")]
    public required string CreatorId { get; set; }

    [BsonElement("backlogs")]
    public List<Backlog> Backlogs { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("participants")]
    public List<Participant> Participants { get; set; } = [];

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("activeTaskId")]
    public string? ActiveTaskId { get; set; }

    public bool IsExpired => CreatedAt > DateTime.Now.AddHours(-3);
    #endregion

    #region Constructor
    internal static Session Create(string sessionName, string userId)
    {
        return new Session
        {
            Id = GenerateRandomId(),
            DisplayName = sessionName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Participants = [],
            CreatorId = userId,
        };
    }

    private static int GenerateRandomId()
    {
        long timeSeed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int randomPart = Random.Shared.Next(0, 100000);
        int combined = (int)((timeSeed + randomPart) % 90000000) + 10000000;
        return combined;
    }
    #endregion
}
