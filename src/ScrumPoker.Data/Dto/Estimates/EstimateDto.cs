namespace ScrumPoker.Data.Dto;

public class EstimateDto
{
    public required string Id { get; set; }
    public required string ParticipantId { get; set; }
    public required string DisplayName { get; set; }
    public int? Value { get; set; }
}
