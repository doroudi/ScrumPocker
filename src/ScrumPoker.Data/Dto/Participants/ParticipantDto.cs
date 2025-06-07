using System;

namespace ScrumPoker.Data.Dto;

public class ParticipantDto
{
    public required string Id { get; set; }
    public long SessionId { get; set; }
    public required string DisplayName { get; set; }
}
