using Microsoft.AspNetCore.Mvc;

public class PhasesController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<PhasesController> _logger;

    public PhasesController(TelemetryDbContext context, ILogger<PhasesController> logger)
    {
        _context = context;
        _logger = logger;
    }
}