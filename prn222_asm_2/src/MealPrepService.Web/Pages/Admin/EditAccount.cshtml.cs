using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditAccountModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<EditAccountModel> _logger;

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = string.Empty;

    [BindProperty]
    public string? Password { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public EditAccountModel(
        IAccountService accountService,
        ILogger<EditAccountModel> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var account = await _accountService.GetByIdAsync(id);

            Id = account.Id;
            Email = account.Email;
            FullName = account.FullName;
            Role = account.Role;

            return Page();
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Admin/Accounts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading account for edit");
            TempData["ErrorMessage"] = "An error occurred while loading the account.";
            return RedirectToPage("/Admin/Accounts");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = new UpdateAccountDto
            {
                Email = Email,
                FullName = FullName,
                Role = Role,
                Password = Password
            };

            var account = await _accountService.UpdateStaffAccountAsync(Id, dto);
            
            TempData["SuccessMessage"] = $"Account updated successfully for {account.FullName}.";
            _logger.LogInformation("Account {AccountId} updated by admin {AdminId}", 
                Id, GetCurrentAccountId());
            
            return RedirectToPage("/Admin/Accounts");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _logger.LogWarning(ex, "Business error updating account");
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while updating the account. Please try again.");
            _logger.LogError(ex, "Unexpected error updating account");
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
