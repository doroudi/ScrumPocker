using MongoDB.Bson;
using ScrumPoker.Data.Dto;

namespace ScrumPoker.Data.Models;

public class Participant
{
    public ObjectId Id { get; init; }
    public long SessionId { get; set; }
    public required string DisplayName { get; set; }
    public DateTime JoinedAtUtc { get; private set; } = DateTime.UtcNow;

    internal static Participant Create(ObjectId id, long sessionId, string displayName)
    {
        return new Participant
        {
            Id = id,
            SessionId = sessionId,
            DisplayName = displayName
        };
    }

    internal ParticipantDto ToDto()
    {
        return new ParticipantDto
        {
            Id = Id.ToString(),
            SessionId = SessionId,
            DisplayName = DisplayName,
        };
    }
}
