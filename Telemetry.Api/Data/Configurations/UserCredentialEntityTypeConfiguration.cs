using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserCredentialEntityTypeConfiguration : IEntityTypeConfiguration<UserCredential>
{
    public void Configure(EntityTypeBuilder<UserCredential> builder)
    {
        builder
            .HasKey(uc => uc.Id);

        builder
            .Property(uc => uc.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder
            .Property(uc => uc.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder
            .Property(uc => uc.VerifyToken)
            .HasMaxLength(128);

        builder
            .Property(uc => uc.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}