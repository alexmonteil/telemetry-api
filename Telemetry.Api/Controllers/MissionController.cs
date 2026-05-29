using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class MissionController : ControllerBase
{
    private readonly TelemetryDbContext _context;
    private readonly ILogger<MissionController> _logger;

    public MissionController(TelemetryDbContext telemetryDbContext, ILogger<MissionController> logger)
    {
        _context = telemetryDbContext;
        _logger = logger;
    }

    // CREATE

    // READ 
    [HttpGet("{id}")]
    public async Task<ActionResult<GetMissionResponse>> GetMission(int id)
    {
        // Search for mission
        var mission = await _context.Missions
                            .Include(m => m.Phases)
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
            Phases = mission.Phases.Select(p => new PhaseSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            }).ToList(),
            TeamMembers = mission.TeamMembers.Select(tm => new TeamMemberSummaryDto
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