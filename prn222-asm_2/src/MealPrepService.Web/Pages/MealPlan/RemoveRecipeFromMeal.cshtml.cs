using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class RemoveRecipeFromMealModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<RemoveRecipeFromMealModel> _logger;

    public RemoveRecipeFromMealModel(IMealPlanService mealPlanService, ILogger<RemoveRecipeFromMealModel> logger)
    {
        _mealPlanService = mealPlanService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid mealId, Guid recipeId, Guid planId)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            await _mealPlanService.RemoveRecipeFromMealAsync(mealId, recipeId, accountId);
            
            _logger.LogInformation("Recipe {RecipeId} removed from meal {MealId} by account {AccountId}", 
                recipeId, mealId, accountId);
            
            TempData["SuccessMessage"] = "Recipe removed from meal successfully!";
            return RedirectToPage("/MealPlan/Details", new { id = planId });
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/MealPlan/Details", new { id = planId });
        }
        catch (AuthorizationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/MealPlan/Details", new { id = planId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing recipe {RecipeId} from meal {MealId}", 
                recipeId, mealId);
            TempData["ErrorMessage"] = "An error occurred while removing the recipe. Please try again.";
            return RedirectToPage("/MealPlan/Details", new { id = planId });
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
