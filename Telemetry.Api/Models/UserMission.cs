public class UserMission
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int MissionId { get; set; }
    public Mission Mission { get; set; } = null!;
}