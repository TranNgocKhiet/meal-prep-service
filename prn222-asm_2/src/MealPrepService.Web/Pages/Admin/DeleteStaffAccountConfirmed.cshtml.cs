using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DeleteStaffAccountConfirmedModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<DeleteStaffAccountConfirmedModel> _logger;

    public DeleteStaffAccountConfirmedModel(
        IAccountService accountService,
        ILogger<DeleteStaffAccountConfirmedModel> logger)
    {
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            var account = await _accountService.GetByIdAsync(id);
            var accountName = account.FullName;
            var accountRole = account.Role;
            
            await _accountService.DeleteStaffAccountAsync(id);
            
            TempData["SuccessMessage"] = $"{accountRole} account for {accountName} deleted successfully.";
            _logger.LogInformation("Staff account {AccountId} deleted by admin {AdminId}", 
                id, GetCurrentAccountId());
            
            return RedirectToPage("/Admin/StaffAccounts");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Business error deleting staff account");
            return RedirectToPage("/Admin/StaffAccounts");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while deleting the account. Please try again.";
            _logger.LogError(ex, "Unexpected error deleting staff account");
            return RedirectToPage("/Admin/StaffAccounts");
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
