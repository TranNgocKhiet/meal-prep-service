using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EditStaffAccountModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<EditStaffAccountModel> _logger;

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

    public EditStaffAccountModel(
        IAccountService accountService,
        ILogger<EditStaffAccountModel> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var account = await _accountService.GetByIdAsync(id);
            
            if (account.Role != "Manager" && account.Role != "DeliveryMan")
            {
                TempData["ErrorMessage"] = "Only Manager and DeliveryMan accounts can be edited.";
                return RedirectToPage("/Admin/StaffAccounts");
            }

            Id = account.Id;
            Email = account.Email;
            FullName = account.FullName;
            Role = account.Role;

            return Page();
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Admin/StaffAccounts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading staff account for edit");
            TempData["ErrorMessage"] = "An error occurred while loading the account.";
            return RedirectToPage("/Admin/StaffAccounts");
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
            
            TempData["SuccessMessage"] = $"Staff account updated successfully for {account.FullName}.";
            _logger.LogInformation("Staff account {AccountId} updated by admin {AdminId}", 
                Id, GetCurrentAccountId());
            
            return RedirectToPage("/Admin/StaffAccounts");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            _logger.LogWarning(ex, "Business error updating staff account");
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "An error occurred while updating the account. Please try again.");
            _logger.LogError(ex, "Unexpected error updating staff account");
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
