using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrumPoker.Data.Models;

public class Estimate
{
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("participantId")]
    public required string ParticipantId { get; init; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("backlogId")]
    public required string BacklogId { get; init; }

    [BsonElement("votedAt")]
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("value")]
    public int Value { get; set; }
}
