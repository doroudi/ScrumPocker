using System;

namespace ScrumPoker.Data.Dto.Backlogs;

public class BacklogEstimateSummaryDto
{
    public string BacklogId { get; set; }
    public List<EstimateDto> Estimates { get; set; }
    public double Average { get; set; }
    public double Mood { get; set; }
}
