using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MissionEntityTypeConfiguration : IEntityTypeConfiguration<Mission>
{
    public void Configure(EntityTypeBuilder<Mission> builder)
    {
        builder
            .HasKey(m => m.Id);

        builder
            .Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder
            .Property(m => m.Description)
            .HasMaxLength(256);

        builder
            .Property(m => m.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}