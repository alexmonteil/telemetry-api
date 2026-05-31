using System.ComponentModel.DataAnnotations;

// INPUT CONTRACTS

public record CreateMissionRequest
{
    [Required(ErrorMessage = "Mission name is required.")]
    [StringLength(128, MinimumLength = 3, ErrorMessage = "Mission name must be between 3 and 128 characters.")]
    public required string Name { get; init; }

    [MaxLength(256, ErrorMessage = "Description cannot exceed 256 characters.")]
    public string? Description { get; init; }
}

public record PutMissionRequest
{
    [Required(ErrorMessage = "Mission name is required.")]
    [StringLength(128, MinimumLength = 3, ErrorMessage = "Mission name must be between 3 and 128 characters.")]
    public required string Name { get; init; }

    [MaxLength(256, ErrorMessage = "Description cannot exceed 256 characters.")]
    public string? Description { get; init; }
}

// OUTPUT CONTRACTS (Responses)

public record GetMissionResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public required User Leader { get; init; }
    public List<PhaseSummary> Phases { get; init; } = [];
    public List<UserSummary> TeamMembers { get; init; } = [];
}

public record CreateMissionResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public required UserSummary Leader { get; init; }
}

public record MissionSummary
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required UserSummary Leader { get; init; }
}
