using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

// The handler evaluates the Requirement against a specific Resource (the Mission)
public class MissionAccessHandler : AuthorizationHandler<MissionAccessRequirement, Mission>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MissionAccessRequirement requirement,
        Mission resource)
    {
        var nameIdentifierClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(nameIdentifierClaim) || !int.TryParse(nameIdentifierClaim, out int authenticatedUserId))
        {
            return Task.CompletedTask; // Leaves the requirement unfulfilled (Unauthorized)
        }

        // The Core Logic: Manager, Leader, or Team Member
        if (roleClaim == UserRole.Manager.ToString() ||
            resource.LeaderId == authenticatedUserId ||
            resource.TeamMembers.Any(tm => tm.UserId == authenticatedUserId))
        {
            // Mark the requirement as successful!
            context.Succeed(requirement);
        }

        // If we reach here and Succeed wasn't called, the user is blocked.
        return Task.CompletedTask;
    }
}