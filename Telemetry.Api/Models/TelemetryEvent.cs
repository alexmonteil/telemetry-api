public class TelemetryEvent
{
    public int Id { get; set; }
    public int PhaseId { get; set; }
    public int? AssigneeId { get; set; }
    public required string Description { get; set; }
    public EventStatus Status { get; set; } = EventStatus.InQueue;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Phase Phase { get; set; } = null!;
    public User? Assignee { get; set; }
}