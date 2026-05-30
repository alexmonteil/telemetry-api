using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class MissionsController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<MissionsController> _logger;

    public MissionsController(TelemetryDbContext telemetryDbContext, ILogger<MissionsController> logger)
    {
        _context = telemetryDbContext;
        _logger = logger;
    }

    // CREATE
    [HttpPost]
    [Authorize(Roles = "Manager")]
    [EndpointSummary("Creates a new mission record.")]
    [ProducesResponseType(typeof(CreateMissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateMissionResponse>> CreateMission([FromBody] CreateMissionRequest req)
    {
        var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usernameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(nameIdentifierClaim) || !int.TryParse(nameIdentifierClaim, out int authenticatedUserId) || string.IsNullOrEmpty(usernameClaim))
        {
            _logger.LogWarning("Failed to create mission: Invalid or missing identity claims.");
            return Problem(
                detail: "An invalid identity signature was detected inside the token payload.",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        var newMission = new Mission
        {
            Name = req.Name,
            Description = req.Description,
            LeaderId = authenticatedUserId
        };

        _context.Missions.Add(newMission);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Mission {MissionId} '{MissionName}' created successfully by user {UserId}.", newMission.Id, newMission.Name, authenticatedUserId);

        return CreatedAtAction(
            nameof(GetMissionById),
            new { id = newMission.Id },
            new CreateMissionResponse
            {
                Id = newMission.Id,
                Name = newMission.Name,
                Description = newMission.Description,
                CreatedAt = newMission.CreatedAt,
                Leader = new UserSummary
                {
                    UserId = newMission.LeaderId,
                    Username = usernameClaim
                }
            }
        );
    }

    // READ 
    [HttpGet("{id:int}")]
    [EndpointSummary("Retrieves a single mission asset specification if it exists.")]
    [ProducesResponseType(typeof(GetMissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetMissionResponse>> GetMissionById(int id)
    {
        var mission = await _context.Missions
            .Include(m => m.Phases)
            .Include(m => m.Leader)
            .Include(m => m.TeamMembers)
                .ThenInclude(tm => tm.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mission == null)
        {
            _logger.LogWarning("Mission with ID {MissionId} was not found.", id);
            return Problem(
                detail: $"Mission with ID {id} could not be found.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        var response = new GetMissionResponse
        {
            Id = mission.Id,
            Name = mission.Name,
            Description = mission.Description ?? string.Empty,
            CreatedAt = mission.CreatedAt,
            Leader = mission.Leader,
            Phases = mission.Phases.Select(p => new PhaseSummary
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            }).ToList(),
            TeamMembers = mission.TeamMembers.Select(tm => new UserSummary
            {
                UserId = tm.User.Id,
                Username = tm.User.Username,
                AvatarUrl = tm.User.AvatarUrl
            }).ToList()
        };

        _logger.LogInformation("Successfully retrieved mission {MissionId}", id);
        return Ok(response);
    }

    // UPDATE: PUT
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager")]
    [EndpointSummary("Overwrites an existing mission record details.")]
    // 💡 TWEAK: Status204NoContent does not return a type payload. Omit typeof(NoContent).
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMission(int id, [FromBody] PutMissionRequest req)
    {
        var mission = await _context.Missions.FirstOrDefaultAsync(m => m.Id == id);

        if (mission == null)
        {
            _logger.LogWarning("Mission with ID {MissionId} was not found.", id);
            return Problem(
                detail: "Target modification mission asset could not be located.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        mission.Name = req.Name;
        mission.Description = req.Description;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully updated mission {MissionId}", id);
        return NoContent();
    }

    // DELETE
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    [EndpointSummary("Removes a mission asset from the database tracking schemas.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMission(int id)
    {
        var mission = await _context.Missions.FirstOrDefaultAsync(m => m.Id == id);

        if (mission == null)
        {
            _logger.LogWarning("Mission with ID {MissionId} was not found.", id);
            return Problem(
                detail: "Target deletion mission asset could not be located.",
                statusCode: StatusCodes.Status404NotFound
            );
        }

        _context.Missions.Remove(mission);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Successfully deleted mission {MissionId}", id);
        return NoContent();
    }
}