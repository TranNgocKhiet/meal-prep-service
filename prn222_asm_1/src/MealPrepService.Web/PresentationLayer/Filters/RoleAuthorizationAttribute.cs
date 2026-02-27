using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace MealPrepService.Web.PresentationLayer.Filters
{
    /// <summary>
    /// Authorization filter that checks if the user has one of the required roles
    /// </summary>
    public class RoleAuthorizationAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _requiredRoles;

        public RoleAuthorizationAttribute(params string[] requiredRoles)
        {
            _requiredRoles = requiredRoles ?? throw new ArgumentNullException(nameof(requiredRoles));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var returnUrl = context.HttpContext.Request.Path.ToString();
                context.Result = new RedirectResult($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }

            // Get user's role
            var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userRole))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Check if user has one of the required roles
            if (!_requiredRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new RedirectResult("/Account/AccessDenied");
                return;
            }
        }
    }

    /// <summary>
    /// Convenience attributes for specific roles
    /// </summary>
    public class AdminOnlyAttribute : RoleAuthorizationAttribute
    {
        public AdminOnlyAttribute() : base("Admin") { }
    }

    public class ManagerOnlyAttribute : RoleAuthorizationAttribute
    {
        public ManagerOnlyAttribute() : base("Manager") { }
    }

    public class CustomerOnlyAttribute : RoleAuthorizationAttribute
    {
        public CustomerOnlyAttribute() : base("Customer") { }
    }

    public class DeliveryManOnlyAttribute : RoleAuthorizationAttribute
    {
        public DeliveryManOnlyAttribute() : base("DeliveryMan") { }
    }

    public class AdminOrManagerAttribute : RoleAuthorizationAttribute
    {
        public AdminOrManagerAttribute() : base("Admin", "Manager") { }
    }

    public class CustomerOrManagerAttribute : RoleAuthorizationAttribute
    {
        public CustomerOrManagerAttribute() : base("Customer", "Manager") { }
    }

    public class AuthenticatedUserAttribute : RoleAuthorizationAttribute
    {
        public AuthenticatedUserAttribute() : base("Admin", "Manager", "Customer", "DeliveryMan") { }
    }
}