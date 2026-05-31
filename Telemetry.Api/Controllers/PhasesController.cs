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

    public PhasesController(TelemetryDbContext context, ILogger<PhasesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // READ
    [HttpGet("{id:int}")]
    [Authorize]
    [EndpointSummary("Retrieves an existing phase record details if it exists.")]
    [ProducesResponseType(typeof(GetPhaseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPhaseResponse>> GetPhaseById(int id)
    {
        var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(nameIdentifierClaim) || !int.TryParse(nameIdentifierClaim, out int authenticatedUserId))
        {
            return Problem(detail: "Invalid identity claims signature.", statusCode: 401);
        }

        var phase = await _context.Phases
            .Include(p => p.Mission)
                .ThenInclude(m => m.Leader)
            .Include(p => p.TelemetryEvents)
            .FirstOrDefaultAsync(p => p.Id == id &&
                (p.Mission.LeaderId == authenticatedUserId ||
                 p.Mission.TeamMembers.Any(tm => tm.UserId == authenticatedUserId)));

        if (phase == null)
        {
            return Problem(detail: $"Phase with ID {id} could not be found.", statusCode: 404);
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

        _logger.LogInformation("Successfully retrieved phase {PhaseId}", id);
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
        // WRITE LOGIC HERE
        return Ok();
    }
}