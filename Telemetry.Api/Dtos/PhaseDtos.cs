// INPUT CONTRACTS

using System.ComponentModel.DataAnnotations;

public record CreatePhaseRequest
{
    public required int MissionId { get; init; }
    [Required(ErrorMessage = "Phase name is required.")]
    [StringLength(128, MinimumLength = 3, ErrorMessage = "Phase name must be between 3 and 128 characters.")]
    public required string Name { get; init; }

    [MaxLength(256, ErrorMessage = "Description cannot exceed 256 characters.")]
    public string? Description { get; init; }
}

public record PutPhaseRequest
{
    [Required(ErrorMessage = "Phase name is required.")]
    [StringLength(128, MinimumLength = 3, ErrorMessage = "Phase name must be between 3 and 128 characters.")]
    public required string Name { get; init; }

    [MaxLength(256, ErrorMessage = "Description cannot exceed 256 characters.")]
    public string? Description { get; init; }
}


// OUTPUT CONTRACTS (Responses)

public record GetPhaseResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    public required int TelemetryEventsCount;
    public required MissionSummary Mission { get; init; }
}

public record CreatePhaseResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int TelemetryEventsCount { get; init; } = 0;
}

public record PhaseSummary
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}