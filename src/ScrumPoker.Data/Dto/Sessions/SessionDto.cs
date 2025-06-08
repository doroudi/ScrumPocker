namespace ScrumPoker.Data.Dto;

public class SessionDto
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public required string CreatorId { get; set; }
    public bool IsExpired { get; set; }
    public required BacklogDto ActiveBacklog { get; set; }
    public List<ParticipantDto> Participants { get; set; } = [];
    public List<BacklogDto>? Backlogs { get; set; } = [];
}
