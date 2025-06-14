using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ScrumPoker.Data.Dto;

namespace ScrumPoker.Data.Models;

public class Estimate
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
    public ObjectId ParticipantId { get; init; }

    public ObjectId BacklogId { get; init; }

    public DateTime VotedAtUtc { get; private set; } = DateTime.UtcNow;
    public int Value { get; set; }

    internal static Estimate Create(string participantId, string backlogId, int value)
    {
        return new Estimate
        {
            ParticipantId = ObjectId.Parse(participantId),
            BacklogId = ObjectId.Parse(backlogId),
            Value = value
        };
    }

    internal EstimateDto ToDto()
    {
        return new EstimateDto
        {
            ParticipantId = ParticipantId.ToString(),
            BacklogId = BacklogId.ToString(),
            Value = Value
        };
    }
}
