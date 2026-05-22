using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// REGISTRATION OF SERVICES WITH DI CONTAINER

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

// Read variables directly from the environment (loaded from your .env file)
var dbHost = "localhost";
var dbPort = "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "enterprisedb";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "devuser";
var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "devpassword";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

// Register PostgreSQL
builder.Services.AddDbContext<TelemetryDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
