using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace MealPrepService.Web.Pages.Account;

[AllowAnonymous]
public class GoogleLoginModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        // Check if Google authentication is configured
        var schemes = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var googleScheme = schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme).Result;
        
        if (googleScheme == null)
        {
            // Google OAuth is not configured, redirect to regular login
            return RedirectToPage("/Account/Login", new { returnUrl = ReturnUrl });
        }

        var redirectUrl = Url.Page("/Account/GoogleResponse", new { ReturnUrl = ReturnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
}
