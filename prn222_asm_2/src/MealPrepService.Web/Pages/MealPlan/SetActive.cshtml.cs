using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class SetActiveModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<SetActiveModel> _logger;

    public SetActiveModel(IMealPlanService mealPlanService, ILogger<SetActiveModel> logger)
    {
        _mealPlanService = mealPlanService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            await _mealPlanService.SetActivePlanAsync(id, accountId);
            
            _logger.LogInformation("Meal plan {MealPlanId} set as active by account {AccountId}", id, accountId);
            
            TempData["SuccessMessage"] = "Meal plan set as active! Your grocery list will now be based on this plan.";
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
            _logger.LogError(ex, "Error occurred while setting meal plan {MealPlanId} as active", id);
            TempData["ErrorMessage"] = "An error occurred while setting the meal plan as active. Please try again.";
            return RedirectToPage("/MealPlan/Index");
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
