public class Phase
{
    public int Id { get; set; }
    public int MissionId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // Navigation
    public List<TelemetryEvent> TelemetryEvents { get; set; } = [];
    public Mission Mission { get; set; } = null!;
}