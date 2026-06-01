// INPUT CONTRACTS

using System.ComponentModel.DataAnnotations;

public record CreateTelemetryEventRequest
{
    public required int PhaseId { get; init; }
    [MaxLength(256, ErrorMessage = "Description cannot exceed 256 characters.")]
    public required string Description { get; init; }
}


// OUTPUT CONTRACTS (Responses)

public record GetTelemetryEventResponse
{
    public required int Id { get; init; }
    public required string Description { get; init; }
    public required EventStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public PhaseSummary? Phase { get; init; }
    public UserSummary? Assignee { get; init; }
}

public record CreateTelemetryEventResponse
{
    public required int Id { get; init; }
    public required string Description { get; init; }
    public required EventStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record TelemetryEventSummary
{
    public required int Id { get; init; }
    public required string Description { get; init; }
    public required EventStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public UserSummary? Assignee { get; init; }
}