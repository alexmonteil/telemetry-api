public class Mission
{
    public int Id { get; set; }
    public required int LeaderId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User Leader { get; set; } = null!;
    public List<Phase> Phases { get; set; } = [];
    public List<UserMission> TeamMembers { get; set; } = [];
}