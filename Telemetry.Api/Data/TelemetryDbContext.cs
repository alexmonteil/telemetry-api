using Microsoft.EntityFrameworkCore;

public class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();
}