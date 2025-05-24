using MongoDB.Bson.Serialization.Attributes;

namespace ScrumPoker.Data.Models;

public class Participant
{
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    [BsonElement("displayName")]
    public string? DisplayName { get; private set; }
    [BsonElement("joinedAt")]
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
}
