namespace ScrumPoker.Data.Dto;

public class BacklogDto
{
    public required string Id { get; set; }
    public string?  Description { get; set; }
    public List<EstimateDto> Estimates { get; set; } = [];
    public bool IsRevealed { get; set; }
}
