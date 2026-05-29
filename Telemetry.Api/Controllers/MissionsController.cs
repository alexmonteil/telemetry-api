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
    [EndpointSummary("Creates a new mission.")]
    [ProducesResponseType(typeof(CreateMissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateMissionResponse>> CreateMission([FromBody] CreateMissionRequest req)
    {
        // Check authorization
        var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usernameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

        // Perform a safety check
        if (string.IsNullOrEmpty(nameIdentifierClaim) || !int.TryParse(nameIdentifierClaim, out int authenticatedUserId) || string.IsNullOrEmpty(usernameClaim))
        {
            _logger.LogWarning("Failed to create mission: Invalid or missing identity claims.");
            return Unauthorized(new OperationStatusResponse(
                false,
                "An invalid identity signature was detected inside the payload."
            ));
        }

        // Create the entity
        var newMission = new Mission
        {
            Name = req.Name,
            Description = req.Description,
            LeaderId = authenticatedUserId
        };

        // Save
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
    [HttpGet("{id}")]
    [EndpointSummary("Retrieves a mission if it exists.")]
    [ProducesResponseType(typeof(GetMissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationStatusResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetMissionResponse>> GetMissionById(int id)
    {
        // Search for mission
        var mission = await _context.Missions
                            .Include(m => m.Phases)
                            .Include(m => m.Leader)
                            .Include(m => m.TeamMembers)
                                .ThenInclude(tm => tm.User)
                            .FirstOrDefaultAsync(m => m.Id == id);

        // Perform checks
        if (mission == null)
        {
            _logger.LogWarning("Mission with ID {MissionId} was not found.", id);
            return NotFound(new OperationStatusResponse(false, $"Mission with ID {id} not found."));
        }


        // Return Dto
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

    // UPDATE


    // DELETE
}