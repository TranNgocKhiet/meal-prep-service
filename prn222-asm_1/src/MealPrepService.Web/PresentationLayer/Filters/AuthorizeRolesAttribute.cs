using Microsoft.AspNetCore.Authorization;

namespace MealPrepService.Web.PresentationLayer.Filters
{
    /// <summary>
    /// Authorization attribute that works with ASP.NET Core's built-in authorization system
    /// This is preferred over custom authorization filters as it integrates better with the framework
    /// </summary>
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }
    }

    /// <summary>
    /// Convenience attributes for specific role combinations
    /// </summary>
    public class AuthorizeAdminAttribute : AuthorizeRolesAttribute
    {
        public AuthorizeAdminAttribute() : base("Admin") { }
    }

    public class AuthorizeManagerAttribute : AuthorizeRolesAttribute
    {
        public AuthorizeManagerAttribute() : base("Manager") { }
    }

    public class AuthorizeCustomerAttribute : AuthorizeRolesAttribute
    {
        public AuthorizeCustomerAttribute() : base("Customer") { }
    }

    public class AuthorizeDeliveryManAttribute : AuthorizeRolesAttribute
    {
        public AuthorizeDeliveryManAttribute() : base("DeliveryMan") { }
    }

    public class AuthorizeAdminOrManagerAttribute : AuthorizeRolesAttribute
    {
        public AuthorizeAdminOrManagerAttribute() : base("Admin", "Manager") { }
    }

    public class AuthorizeCustomerOrManagerAttribute : AuthorizeRolesAttribute
    {
        public AuthorizeCustomerOrManagerAttribute() : base("Customer", "Manager") { }
    }
}