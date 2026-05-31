using Microsoft.AspNetCore.Mvc;

public class TelemetryEventsController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<TelemetryEventsController> _logger;

    public TelemetryEventsController(TelemetryDbContext context, ILogger<TelemetryEventsController> logger)
    {
        _context = context;
        _logger = logger;
    }
}