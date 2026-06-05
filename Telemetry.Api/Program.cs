using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Serilog;

DotNetEnv.Env.TraversePath().Load();

// 1. Initialize Logger Early
Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

try
{
    Log.Information("Starting up Telemetry Web API Engine...");

    var builder = WebApplication.CreateBuilder(args);

    // Enable Serilog instead of default Logger
    builder.Host.UseSerilog();

    // Bind mail environment vars to MailSettings class
    builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

    // Register MailService;
    builder.Services.AddTransient<IMailService, DefaultMailService>();

    // Read environment variables for JWT auth
    var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
        ?? throw new InvalidOperationException("Critical Failure: JWT_SECRET_KEY environment variable is not set.");
    var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "TelemetryDefaultIssuer";
    var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "TelemetryDefaultAudience";

    // Register JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

    // Build DB Connection String
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "enterprisedb";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "devuser";
    var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "devpassword";

    var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

    builder.Services.AddDbContext<TelemetryDbContext>(options => options.UseNpgsql(connectionString));

    // Register Custom Authorization Handlers & Policies
    builder.Services.AddSingleton<IAuthorizationHandler, MissionAccessHandler>();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("MissionAccessPolicy", policy => policy.Requirements.Add(new MissionAccessRequirement()));
    });

    builder.Services.AddControllers();

    // Register DatabaseSeeder as a scoped service
    builder.Services.AddScoped<DatabaseSeeder>();
    builder.Services.AddOpenApi();

    // Register the custom Global Exception Handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // TWEAK 1: Clean up internal HTTP logging noise (Place BEFORE controller mapping)
    app.UseSerilogRequestLogging();

    app.UseExceptionHandler(); // Maps the IExceptionHandler middleware into the pipeline

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        // Execute Database Seeding 
        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            // Blocks the startup pipeline intentionally until data is seeded
            seeder.SeedAsync().GetAwaiter().GetResult();
        }
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application host terminated unexpectedly during initialization.");
}
finally
{
    // TWEAK 2: Forces Serilog to dump remaining memory streams to disk before app dies
    Log.CloseAndFlush();
}