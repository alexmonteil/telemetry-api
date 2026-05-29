using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MissionController : ControllerBase
{
    private readonly TelemetryDbContext _context;

    public MissionController(TelemetryDbContext telemetryDbContext)
    {
        _context = telemetryDbContext;
    }

    // CREATE

    // READ 

    // UPDATE

    // DELETE
}