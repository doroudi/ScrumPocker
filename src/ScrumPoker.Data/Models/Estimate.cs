namespace ScrumPoker.Data.Models;

public class Estimate
{
    public string ParticipantId { get; set; }
    public string BacklogId { get; set; }
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    public int EstimatedValue { get; set; }
}
