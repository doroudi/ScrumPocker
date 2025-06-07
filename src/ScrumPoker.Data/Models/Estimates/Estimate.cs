using MongoDB.Bson;

namespace ScrumPoker.Data.Models;

public class Estimate
{
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
}
