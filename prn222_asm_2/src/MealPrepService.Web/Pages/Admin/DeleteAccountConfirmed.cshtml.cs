using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DeleteAccountConfirmedModel : PageModel
{
    private readonly IAccountService _accountService;
    private readonly ILogger<DeleteAccountConfirmedModel> _logger;

    public DeleteAccountConfirmedModel(
        IAccountService accountService,
        ILogger<DeleteAccountConfirmedModel> logger)
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
            _logger.LogInformation("Account {AccountId} deleted by admin {AdminId}", 
                id, GetCurrentAccountId());
            
            return RedirectToPage("/Admin/Accounts");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Business error deleting account");
            return RedirectToPage("/Admin/Accounts");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while deleting the account. Please try again.";
            _logger.LogError(ex, "Unexpected error deleting account");
            return RedirectToPage("/Admin/Accounts");
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
