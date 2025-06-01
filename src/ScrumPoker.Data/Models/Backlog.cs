using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ScrumPoker.Data.Models;

public class Backlog
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; init; }
    [BsonElement("title")]
    public string? Title { get; private set; }
    [BsonElement("estimates")]
    public List<Estimate> Estimates { get; private set; } = [];

    [BsonElement("finalEstimate")]
    public double FinalEstimate { get; private set; }

    public bool IsRevealed { get; private set; }

    public void Initialize(List<string> userIds) 
    {
        if (userIds == null || userIds.Count == 0)
            throw new ArgumentException("User IDs cannot be null or empty.", nameof(userIds));

        Estimates = userIds.Select(userId => new Estimate { ParticipantId = userId, BacklogId = "" }).ToList();
        IsRevealed = false;
    }

    public void Reveal()
    {
        if (IsRevealed)
            throw new InvalidOperationException("Backlog is already revealed.");

        IsRevealed = true;
        FinalEstimate = Estimates.Average(e => e.Value);
    }

    public void SetEstimate(string userId, int estimatedValue)
    {
        if (IsRevealed)
            throw new InvalidOperationException("Cannot add estimate to a revealed backlog.");

        var estimate = Estimates.FirstOrDefault(x => x.ParticipantId == userId);
        if (estimate == null)
        {
            estimate = new Estimate { ParticipantId = userId, Value = estimatedValue, BacklogId  = "" };
            Estimates.Add(estimate);
        }
        else
        {
            estimate.Value = estimatedValue;
        }
        
    }
}
