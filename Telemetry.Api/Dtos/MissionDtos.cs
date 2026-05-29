using System.ComponentModel.DataAnnotations;

// INPUT CONTRACTS

// OUTPUT CONTRACTS (Responses)

public record GetMissionResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<PhaseSummaryDto> Phases { get; init; } = [];
    public List<TeamMemberSummaryDto> TeamMembers { get; init; } = [];
}

public record PhaseSummaryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public record TeamMemberSummaryDto
{
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public string? AvatarUrl { get; init; }
}