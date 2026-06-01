using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class TelemetryEventsController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<TelemetryEventsController> _logger;

    public TelemetryEventsController(TelemetryDbContext context, ILogger<TelemetryEventsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // READ
    [HttpGet("{id:int}")]
    [Authorize]
    [EndpointSummary("Retrieves an existing telemetry event record details if it exists.")]
    [ProducesResponseType(typeof(GetTelemetryEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTelemetryEventResponse>> GetTelemetryEventById(int id)
    {

        var telEvent = await _context.TelemetryEvents
                .Include(te => te.Phase)
                .Include(te => te.Assignee)
                .FirstOrDefaultAsync(te => te.Id == id);

        if (telEvent == null)
        {
            _logger.LogWarning("Telemetry event with ID: {TelemetryEventId} was not found.", id);
            return Problem(detail: $"Telemetry event with ID {id} could not be found.", statusCode: 404);
        }

        // Map phase to dto
        var response = new GetTelemetryEventResponse
        {
            Id = telEvent.Id,
            Description = telEvent.Description,
            Status = telEvent.Status,
            CreatedAt = telEvent.CreatedAt,
            Phase = new PhaseSummary
            {
                Id = telEvent.PhaseId,
                Name = telEvent.Phase.Name,
                Description = telEvent.Phase.Description
            },
            Assignee = telEvent.Assignee != null ? new UserSummary
            {
                UserId = telEvent.Assignee.Id,
                Username = telEvent.Assignee.Username,
                AvatarUrl = telEvent.Assignee.AvatarUrl
            } : null
        };

        _logger.LogInformation("Successfully retrieved telemetry event {TelemetryEventId}.", id);
        return Ok(response);
    }

    // CREATE
    [HttpPost]
    [Authorize]
    [EndpointSummary("Creates a new Telemetry event record.")]
    [ProducesResponseType(typeof(CreateMissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateMissionResponse>> CreateTelemetryEvent([FromBody] CreateTelemetryEventRequest req)
    {
        var newEvent = new TelemetryEvent
        {
            PhaseId = req.PhaseId,
            Description = req.Description
        };

        _context.TelemetryEvents.Add(newEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Mission {MissionId} created successfully.", newEvent.Id);

        return CreatedAtAction(
            nameof(GetTelemetryEventById),
            new { id = newEvent.Id },
            new CreateTelemetryEventResponse
            {
                Id = newEvent.Id,
                Description = newEvent.Description,
                Status = newEvent.Status,
                CreatedAt = newEvent.CreatedAt
            }
        );
    }

    // UPDATE

    // DELETE

}