using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Read variables directly from the environment (loaded from your .env file)
var dbHost = "localhost";
var dbPort = "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "enterprisedb";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "devuser";
var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "devpassword";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

// Add services to the container.

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

app.UseAuthorization();

app.MapControllers();

app.Run();
