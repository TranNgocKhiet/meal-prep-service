using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IAccountService accountService, ILogger<LoginModel> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var accountDto = await _accountService.AuthenticateAsync(Email, Password);
            
            _logger.LogInformation("User {Email} logged in successfully", Email);

            await SignInUserAsync(accountDto, RememberMe);

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Index");
        }
        catch (AuthenticationException)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for {Email}", Email);
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            return Page();
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
