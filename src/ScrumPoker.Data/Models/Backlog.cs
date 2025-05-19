namespace ScrumPoker.Data.Models;

public class Backlog
{
    public Guid Id { get; private set; }
    public string? Title { get; private set; }
    public List<Estimate> Estimates { get; private set; } = [];
    public int FinalEstimate { get; private set; }
}
