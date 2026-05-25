using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TelemetryEventEntityTypeConfiguration : IEntityTypeConfiguration<TelemetryEvent>
{
    public void Configure(EntityTypeBuilder<TelemetryEvent> builder)
    {
        builder
            .HasKey(te => te.Id);


        // Assignee - TelemetryEvent Relationship: One to Many
        builder
            .HasOne(te => te.Assignee)
            .WithMany(a => a.AssignedEvents)
            .HasForeignKey(te => te.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Phase - TelemetryEvent Relationship: One to Many
        builder
            .HasOne(te => te.Phase)
            .WithMany(p => p.TelemetryEvents)
            .HasForeignKey(te => te.PhaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(te => te.Description)
            .IsRequired()
            .HasMaxLength(256);

        builder
            .Property(te => te.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(EventStatus.InQueue);

        builder
            .Property(te => te.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}