using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrumPoker.Data.Models;

public class Estimate
{
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("participantId")]
    public string ParticipantId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("backlogId")]
    public string BacklogId { get; set; }

    [BsonElement("votedAt")]
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("value")]
    public int Value { get; set; }
}
