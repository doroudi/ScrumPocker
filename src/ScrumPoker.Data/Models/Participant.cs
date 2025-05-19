namespace ScrumPoker.Data.Models;

public class Participant
{
    public Participant()
    {
    }

    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string? DisplayName { get; private set; }
    public DateTime JoinedAt { get; private set; } = DateTime.UtcNow;
}
