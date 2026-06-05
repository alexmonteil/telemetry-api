using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


public class TelemetryEventsController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<TelemetryEventsController> _logger;
    private readonly IAuthorizationService _authorizationService;

    public TelemetryEventsController(TelemetryDbContext context, ILogger<TelemetryEventsController> logger, IAuthorizationService authorizationService)
    {
        _context = context;
        _logger = logger;
        _authorizationService = authorizationService;
    }

    // READ
    [HttpGet("{id:int}")]
    [Authorize]
    [EndpointSummary("Retrieves an existing telemetry event record details if it exists.")]
    [ProducesResponseType(typeof(GetTelemetryEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetTelemetryEventResponse>> GetTelemetryEventById(int id)
    {
        // 1. Fetch the data cleanly
        var telEvent = await _context.TelemetryEvents
                .Include(te => te.Phase)
                    .ThenInclude(p => p.Mission)
                        .ThenInclude(m => m.TeamMembers)
                .Include(te => te.Assignee)
                .FirstOrDefaultAsync(te => te.Id == id);

        if (telEvent == null)
        {
            _logger.LogWarning("Telemetry event with ID: {TelemetryEventId} was not found.", id);
            return Problem(detail: $"Telemetry event with ID {id} could not be found.", statusCode: 404);
        }

        // 2. Perform Imperative Authorization against the underlying Mission
        var authResult = await _authorizationService.AuthorizeAsync(User, telEvent.Phase.Mission, "MissionAccessPolicy");
        if (!authResult.Succeeded)
        {
            return Forbid(); // Automatically returns a 403 Forbidden response
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
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateMissionResponse>> CreateTelemetryEvent([FromBody] CreateTelemetryEventRequest req)
    {
        var phase = await _context.Phases
            .Include(p => p.Mission)
                .ThenInclude(m => m.TeamMembers)
            .FirstOrDefaultAsync(p => p.Id == req.PhaseId);

        if (phase == null)
        {
            _logger.LogWarning("Bad request attempting to insert telemetry event, no phase with ID: {PhaseId} exists.", req.PhaseId);
            return Problem(
                detail: $"Phase with ID: {req.PhaseId} does not exist.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        var authResult = await _authorizationService.AuthorizeAsync(User, phase.Mission, "MissionAccessPolicy");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var newEvent = new TelemetryEvent
        {
            PhaseId = req.PhaseId,
            Description = req.Description
        };

        _context.TelemetryEvents.Add(newEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Telemetry event {TelemetryEventId} created successfully.", newEvent.Id);

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
    [HttpPut("{id:int}")]
    [Authorize]
    [EndpointSummary("Overwrites an existing telemetry event record details.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTelemetryEvent(int id, [FromBody] UpdateTelemetryEventRequest req)
    {
        var telEvent = await _context.TelemetryEvents
                        .Include(te => te.Phase)
                            .ThenInclude(p => p.Mission)
                                .ThenInclude(m => m.TeamMembers)
                        .FirstOrDefaultAsync(te => te.Id == id);

        if (telEvent == null)
        {
            _logger.LogWarning("Telemetry event with ID: {TelemetryEventId} was not found.", id);
            return Problem(
                detail: $"Telemetry event with ID: {id} could not be found.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        var authResult = await _authorizationService.AuthorizeAsync(User, telEvent.Phase.Mission, "MissionAccessPolicy");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        // Update record
        telEvent.Description = req.Description;
        telEvent.Status = req.Status;

        // Save
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully updated telemetry event with ID: {TelemetryEventId}.", id);
        return NoContent();
    }

    // DELETE
    [HttpDelete("{id:int}")]
    [Authorize]
    [EndpointSummary("Removes a telemetry event asset from the database.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTelemetryEvent(int id)
    {
        var telEvent = await _context.TelemetryEvents
                        .Include(te => te.Phase)
                            .ThenInclude(p => p.Mission)
                                .ThenInclude(m => m.TeamMembers)
                        .FirstOrDefaultAsync(te => te.Id == id);

        if (telEvent == null)
        {
            _logger.LogWarning("Telemetry event with ID: {TelemetryEventId} was not found.", id);
            return Problem(
                detail: $"Telemetry event with ID: {id} could not be found.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        var authResult = await _authorizationService.AuthorizeAsync(User, telEvent.Phase.Mission, "MissionAccessPolicy");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        _context.TelemetryEvents.Remove(telEvent);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully deleted telemetry event with ID: {TelemetryEventId}.", id);
        return NoContent();
    }
}