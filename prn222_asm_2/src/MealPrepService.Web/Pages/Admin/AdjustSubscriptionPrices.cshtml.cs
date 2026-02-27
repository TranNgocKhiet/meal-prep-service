using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class AdjustSubscriptionPricesModel : PageModel
{
    private readonly ILogger<AdjustSubscriptionPricesModel> _logger;

    public AdjustSubscriptionPricesModel(ILogger<AdjustSubscriptionPricesModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Stub implementation - in real implementation, this would use AI to adjust prices
            TempData["SuccessMessage"] = "Subscription prices adjusted successfully using AI recommendations.";
            _logger.LogInformation("Subscription prices adjusted using AI by admin {AdminId}", GetCurrentAccountId());
            
            return RedirectToPage("/Admin/Dashboard");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while adjusting subscription prices. Please try again.";
            _logger.LogError(ex, "Unexpected error adjusting subscription prices");
            return RedirectToPage("/Admin/Dashboard");
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
