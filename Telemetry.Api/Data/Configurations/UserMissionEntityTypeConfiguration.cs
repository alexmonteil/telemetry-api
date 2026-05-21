using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserMissionEntityTypeConfiguration : IEntityTypeConfiguration<UserMission>
{
    public void Configure(EntityTypeBuilder<UserMission> builder)
    {
        builder
            .HasKey(um => new { um.UserId, um.MissionId });

        // User - Mission Relationship: Many to Many
        builder
            .HasOne(um => um.User)
            .WithMany(u => u.UserMissions)
            .HasForeignKey(um => um.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(um => um.Mission)
            .WithMany(m => m.TeamMembers)
            .HasForeignKey(t => t.MissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}