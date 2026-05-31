// INPUT CONTRACTS


// OUTPUT CONTRACTS (Responses)

public record UserSummary
{
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public string? AvatarUrl { get; init; }
}