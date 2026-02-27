using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CreateStaffAccountModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<CreateStaffAccountModel> _logger;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = "Manager";

    public CreateStaffAccountModel(
        IAccountService accountService,
        ILogger<CreateStaffAccountModel> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IActionResult OnGet()
    {
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
            var dto = new CreateAccountDto
            {
                Email = Email,
                Password = Password,
                FullName = FullName
            };

            var account = await _accountService.CreateStaffAccountAsync(dto, Role);
            
            TempData["SuccessMessage"] = $"{Role} account created successfully for {account.FullName}.";
            _logger.LogInformation("{Role} account created by admin {AdminId} for {Email}", 
                Role, GetCurrentAccountId(), account.Email);
            
            return RedirectToPage("/Admin/StaffAccounts");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _logger.LogWarning(ex, "Business error creating staff account");
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while creating the account. Please try again.");
            _logger.LogError(ex, "Unexpected error creating staff account");
            return Page();
        }
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new AuthenticationException("User account ID not found in claims.");
        }
        return accountId;
    }
}
