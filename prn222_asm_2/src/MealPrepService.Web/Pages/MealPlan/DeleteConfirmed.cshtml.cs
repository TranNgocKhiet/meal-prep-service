using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class DeleteConfirmedModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<DeleteConfirmedModel> _logger;

    public DeleteConfirmedModel(
        IMealPlanService mealPlanService,
        ILogger<DeleteConfirmedModel> logger)
    {
        _mealPlanService = mealPlanService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            await _mealPlanService.DeleteAsync(id, accountId);
            
            _logger.LogInformation("Meal plan {MealPlanId} deleted successfully by account {AccountId}", id, accountId);
            
            TempData["SuccessMessage"] = "Meal plan deleted successfully!";
            return RedirectToPage("/MealPlan/Index");
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/MealPlan/Index");
        }
        catch (AuthorizationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/MealPlan/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting meal plan {MealPlanId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the meal plan. Please try again.";
            return RedirectToPage("/MealPlan/Details", new { id });
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
