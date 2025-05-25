using MongoDB.Bson.Serialization.Attributes;

namespace ScrumPoker.Data.Models;

public class Participant
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string? Id { get; init; }
    [BsonElement("displayName")]
    public string? DisplayName { get; set; }
    [BsonElement("joinedAt")]
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
}
