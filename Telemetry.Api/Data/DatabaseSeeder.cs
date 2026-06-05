using Microsoft.EntityFrameworkCore;

using BC = BCrypt.Net.BCrypt;

public class DatabaseSeeder
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(TelemetryDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Ensure the database is created and migrations are applied
        await _context.Database.MigrateAsync();

        if (!await _context.Users.AnyAsync())
        {
            _logger.LogInformation("No users found in database. Seeding default test accounts...");

            var testUsers = new List<User>
            {
                new User
                {
                    Username = "manager_tester",
                    Email = "manager@telemetry.local",
                    IsEmailVerified = true,
                    Role = UserRole.Manager,
                    UserCredential = new UserCredential
                    {
                        PasswordHash = BC.HashPassword("Manager123!")
                    }
                },
                new User
                {
                    Username = "user_tester",
                    Email = "user@telemetry.local",
                    IsEmailVerified = true,
                    Role = UserRole.User,
                    UserCredential = new UserCredential
                    {
                        PasswordHash = BC.HashPassword("User123!")
                    }
                }
            };

            _context.Users.AddRange(testUsers);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded test accounts.");
        }
    }
}