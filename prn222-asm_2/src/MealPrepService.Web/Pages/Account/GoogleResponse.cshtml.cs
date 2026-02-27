using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.Pages.Account;

[AllowAnonymous]
public class GoogleResponseModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<GoogleResponseModel> _logger;

    public GoogleResponseModel(IAccountService accountService, ILogger<GoogleResponseModel> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                _logger.LogWarning("Google authentication failed");
                return RedirectToPage("/Account/Login");
            }

            var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
            {
                _logger.LogWarning("Google authentication succeeded but email or name is missing");
                return RedirectToPage("/Account/Login");
            }

            // Check if user already exists
            var existingUser = await _accountService.EmailExistsAsync(email);
            AccountDto accountDto;

            if (!existingUser)
            {
                // Create new account for Google user
                var createAccountDto = new CreateAccountDto
                {
                    Email = email,
                    Password = Guid.NewGuid().ToString(), // Random password for Google users
                    FullName = name
                };

                accountDto = await _accountService.RegisterAsync(createAccountDto);
                _logger.LogInformation("New Google user {Email} registered successfully", email);
            }
            else
            {
                // Get existing user
                _logger.LogInformation("Existing Google user {Email} logged in", email);
                accountDto = new AccountDto
                {
                    Email = email,
                    FullName = name,
                    Role = "Customer" // Default role
                };
            }

            await SignInUserAsync(accountDto, false);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Google authentication");
            return RedirectToPage("/Account/Login");
        }
    }

    private async Task SignInUserAsync(AccountDto account, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.FullName),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(1)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);
    }
}
