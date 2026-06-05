using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class PhasesController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<PhasesController> _logger;
    private readonly IAuthorizationService _authorizationService;

    public PhasesController(TelemetryDbContext context, ILogger<PhasesController> logger, IAuthorizationService authorizationService)
    {
        _context = context;
        _logger = logger;
        _authorizationService = authorizationService;
    }

    // READ
    [HttpGet("{id:int}")]
    [Authorize]
    [EndpointSummary("Retrieves an existing phase record details if it exists.")]
    [ProducesResponseType(typeof(GetPhaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPhaseResponse>> GetPhaseById(int id)
    {
        var phase = await _context.Phases
            .Include(p => p.Mission)
                .ThenInclude(m => m.TeamMembers)
            .Include(p => p.Mission)
                .ThenInclude(m => m.Leader)
            .Include(p => p.TelemetryEvents)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (phase == null)
        {
            return Problem(detail: $"Phase with ID {id} could not be found.", statusCode: StatusCodes.Status404NotFound);
        }

        var authResult = await _authorizationService.AuthorizeAsync(User, phase.Mission, "MissionAccessPolicy");
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        // Map phase to dto
        var response = new GetPhaseResponse
        {
            Id = phase.Id,
            Name = phase.Name,
            Description = phase.Description,
            TelemetryEventsCount = phase.TelemetryEvents.Count,
            Mission = new MissionSummary
            {
                Id = phase.MissionId,
                Name = phase.Mission.Name,
                Leader = new UserSummary
                {
                    UserId = phase.Mission.LeaderId,
                    Username = phase.Mission.Leader.Username,
                    AvatarUrl = phase.Mission.Leader.AvatarUrl
                }
            }
        };

        _logger.LogInformation("Successfully retrieved phase {PhaseId}.", id);
        return Ok(response);
    }

    // CREATE
    [HttpPost]
    [Authorize(Roles = "Manager")]
    [EndpointSummary("Creates a new phase.")]
    [ProducesResponseType(typeof(CreatePhaseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatePhaseResponse>> CreatePhase([FromBody] CreatePhaseRequest req)
    {
        var missionExists = await _context.Missions.AnyAsync(m => m.Id == req.MissionId);

        if (!missionExists)
        {
            _logger.LogWarning("Bad request attempting to insert phase, no mission with ID: {PhaseId} exists.", req.MissionId);
            return Problem(
                detail: $"Mission with ID: {req.MissionId} does not exist.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        // Create new phase
        var newPhase = new Phase
        {
            MissionId = req.MissionId,
            Name = req.Name,
            Description = req.Description
        };

        // Save
        _context.Phases.Add(newPhase);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Phase {PhaseId} '{PhaseName}' created successfully.", newPhase.Id, newPhase.Name);

        return CreatedAtAction(
            nameof(GetPhaseById),
            new { Id = newPhase.Id },
            new CreatePhaseResponse
            {
                Id = newPhase.Id,
                Name = newPhase.Name,
                Description = newPhase.Description
            }
        );
    }

    // UPDATE: PUT
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager")]
    [EndpointSummary("Overwrites an existing phase record details.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePhase(int id, [FromBody] PutPhaseRequest req)
    {
        // Search for phase
        var phase = await _context.Phases.FirstOrDefaultAsync(p => p.Id == id);
        if (phase == null)
        {
            _logger.LogWarning("Phase with ID: {PhaseId} was not found.", id);
            return Problem(
                detail: $"Phase with ID: {id} could not be located.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        // Update phase
        phase.Name = req.Name;
        phase.Description = req.Description;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully updated phase with ID: {PhaseId}.", id);
        return NoContent();
    }

    // DELETE
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    [EndpointSummary("Removes a phase asset from the database.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhase(int id)
    {
        var phase = await _context.Phases.FirstOrDefaultAsync(p => p.Id == id);

        if (phase == null)
        {
            _logger.LogWarning("Phase with ID: {PhaseId} was not found.", id);
            return Problem(
                detail: $"Phase with ID: {id} could not be found.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        _context.Phases.Remove(phase);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully deleted phase with ID: {PhaseId}.", id);
        return NoContent();
    }
}