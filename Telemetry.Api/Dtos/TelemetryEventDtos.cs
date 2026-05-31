// INPUT CONTRACTS

// OUTPUT CONTRACTS (Responses)

public record TelemetryEventSummary
{
    public required int Id { get; init; }
    public required string Description { get; init; }
    public required EventStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public User? Assignee { get; init; }
}