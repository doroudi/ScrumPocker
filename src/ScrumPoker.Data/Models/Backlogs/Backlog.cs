using MongoDB.Bson;
using ScrumPoker.Data.Dto;

namespace ScrumPoker.Data.Models;

public class Backlog
{
    public ObjectId Id { get; init; }
    public long SessionId { get; set; }
    public string? Description { get; set; }

    public double FinalEstimate { get; private set; }

    public bool IsRevealed { get; private set; }

    public static Backlog Create(long sessionId, string? description = null)
    {
        return new Backlog
        {
            Id = ObjectId.GenerateNewId(),
            SessionId = sessionId,
            Description = description,
            IsRevealed = false,
        };
    }

    public void Reveal()
    {
        if (IsRevealed)
            throw new InvalidOperationException("Backlog is already revealed.");

        IsRevealed = true;
    }

    internal BacklogDto ToDto()
    {
        return new BacklogDto
        {
            Id = Id.ToString(),
            Description = Description,
            IsRevealed = IsRevealed
        };
    }
}
