using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MealPreparationService.API.Authorization;

public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(ClaimTypes.Role);
        
        if (roleClaim != null && requirement.AllowedRoles.Contains(roleClaim.Value))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
