using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PhaseEntityTypeConfiguration : IEntityTypeConfiguration<Phase>
{
    public void Configure(EntityTypeBuilder<Phase> builder)
    {
        builder
            .HasKey(p => p.Id);

        builder
            .Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder
            .Property(p => p.Description)
            .HasMaxLength(256);

        // Mission - Phase Relationship: One to Many
        builder
            .HasOne(p => p.Mission)
            .WithMany(m => m.Phases)
            .HasForeignKey(p => p.MissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}