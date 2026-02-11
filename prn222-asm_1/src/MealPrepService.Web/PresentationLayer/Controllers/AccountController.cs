Note: You causing Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var createAccountDto = new CreateAccountDto
                {
                    Email = model.Email,
                    Password = model.Password,
                    FullName = model.FullName
                };

                var accountDto = await _accountService.RegisterAsync(createAccountDto);
                
                _logger.LogInformation("User {Email} registered successfully", model.Email);

                // Sign in the user after successful registration
                await SignInUserAsync(accountDto, model.RememberMe);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var accountDto = await _accountService.AuthenticateAsync(model.Email, model.Password);
                
                _logger.LogInformation("User {Email} logged in successfully", model.Email);

                await SignInUserAsync(accountDto, model.RememberMe);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (AuthenticationException)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User {Email} logged out", User.Identity?.Name);
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            // Check if Google authentication is configured
            var schemes = HttpContext.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var googleScheme = schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme).Result;
            
            if (googleScheme == null)
            {
                // Google OAuth is not configured, redirect to regular login
                return RedirectToAction("Login", new { returnUrl });
            }

            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Google authentication failed");
                    return RedirectToAction("Login");
                }

                var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("Google authentication succeeded but email or name is missing");
                    return RedirectToAction("Login");
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
                    // Get existing user (we can't authenticate with password for Google users)
                    // This is a simplified approach - in production, you'd want to link Google accounts properly
                    _logger.LogInformation("Existing Google user {Email} logged in", email);
                    // For now, we'll create a basic AccountDto - this should be improved in production
                    accountDto = new AccountDto
                    {
                        Email = email,
                        FullName = name,
                        Role = "Customer" // Default role
                    };
                }

                await SignInUserAsync(accountDto, false);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Google authentication");
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
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
}