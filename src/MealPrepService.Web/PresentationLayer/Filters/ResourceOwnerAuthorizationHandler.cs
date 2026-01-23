using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MealPrepService.Web.PresentationLayer.Filters
{
    /// <summary>
    /// Authorization requirement for resource ownership
    /// </summary>
    public class ResourceOwnerRequirement : IAuthorizationRequirement
    {
        public string ResourceIdParameterName { get; }

        public ResourceOwnerRequirement(string resourceIdParameterName = "id")
        {
            ResourceIdParameterName = resourceIdParameterName;
        }
    }

    /// <summary>
    /// Authorization handler that checks if the user owns the resource or has admin/manager role
    /// </summary>
    public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ResourceOwnerAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var user = context.User;
            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // Admin and Manager can access any resource
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Admin" || userRole == "Manager")
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // For other users, check if they own the resource
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // Get the resource ID from route parameters
            var routeData = httpContext.GetRouteData();
            var resourceIdValue = routeData.Values[requirement.ResourceIdParameterName]?.ToString();

            if (string.IsNullOrEmpty(resourceIdValue))
            {
                // If no resource ID in route, check query parameters
                resourceIdValue = httpContext.Request.Query[requirement.ResourceIdParameterName].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(resourceIdValue))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // For simplicity, we'll assume the resource ID matches the user ID
            // In a real application, you'd query the database to check ownership
            if (resourceIdValue.Equals(userId, StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Authorization attribute for resource ownership
    /// </summary>
    public class AuthorizeResourceOwnerAttribute : AuthorizeAttribute
    {
        public AuthorizeResourceOwnerAttribute(string resourceIdParameterName = "id")
        {
            Policy = $"ResourceOwner_{resourceIdParameterName}";
        }
    }
}