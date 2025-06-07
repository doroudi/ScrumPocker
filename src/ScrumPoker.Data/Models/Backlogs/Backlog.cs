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

    public static Backlog Create()
    {
        return new Backlog
        {
            Id = ObjectId.GenerateNewId(),
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
