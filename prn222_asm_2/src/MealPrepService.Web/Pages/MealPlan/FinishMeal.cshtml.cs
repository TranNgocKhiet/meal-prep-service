using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class FinishMealModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<FinishMealModel> _logger;

    public FinishMealModel(IMealPlanService mealPlanService, IFridgeService fridgeService, ILogger<FinishMealModel> logger)
    {
        _mealPlanService = mealPlanService;
        _fridgeService = fridgeService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid mealId, Guid planId, bool forceComplete = false)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var mealPlan = await _mealPlanService.GetByIdAsync(planId);
            
            if (mealPlan == null || mealPlan.AccountId != accountId)
            {
                TempData["ErrorMessage"] = "Meal plan not found or access denied.";
                return RedirectToPage("/MealPlan/Details", new { id = planId });
            }

            var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);
            if (meal == null)
            {
                TempData["ErrorMessage"] = "Meal not found.";
                return RedirectToPage("/MealPlan/Details", new { id = planId });
            }

            if (meal.MealFinished)
            {
                TempData["InfoMessage"] = "This meal has already been finished.";
                return RedirectToPage("/MealPlan/Details", new { id = planId });
            }

            var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            var fridgeItemsList = fridgeItems.OrderBy(f => f.ExpiryDate).ToList();

            var requiredIngredients = new Dictionary<Guid, float>();
            foreach (var recipe in meal.Recipes)
            {
                if (recipe.Ingredients != null)
                {
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        if (requiredIngredients.ContainsKey(ingredient.IngredientId))
                        {
                            requiredIngredients[ingredient.IngredientId] += ingredient.Amount;
                        }
                        else
                        {
                            requiredIngredients[ingredient.IngredientId] = ingredient.Amount;
                        }
                    }
                }
            }

            var updatedCount = 0;
            var removedCount = 0;
            foreach (var required in requiredIngredients)
            {
                var ingredientId = required.Key;
                var requiredAmount = required.Value;

                var availableItems = fridgeItemsList
                    .Where(f => f.IngredientId == ingredientId)
                    .OrderBy(f => f.ExpiryDate)
                    .ToList();

                var remainingRequired = requiredAmount;

                foreach (var fridgeItem in availableItems)
                {
                    if (remainingRequired <= 0) break;

                    var amountToConsume = Math.Min(fridgeItem.CurrentAmount, remainingRequired);
                    var newAmount = fridgeItem.CurrentAmount - amountToConsume;
                    remainingRequired -= amountToConsume;

                    if (newAmount <= 0.001f)
                    {
                        await _fridgeService.RemoveItemAsync(fridgeItem.Id);
                        removedCount++;
                    }
                    else
                    {
                        await _fridgeService.UpdateItemQuantityAsync(fridgeItem.Id, newAmount);
                    }
                    updatedCount++;
                }
            }

            await _mealPlanService.MarkMealAsFinishedAsync(mealId, accountId, true);

            _logger.LogInformation("Meal {MealId} marked as finished. {Count} fridge items updated ({Removed} removed) for account {AccountId}", 
                mealId, updatedCount, removedCount, accountId);
            
            var successMessage = $"Meal finished! {updatedCount} ingredients consumed from your fridge";
            if (removedCount > 0)
            {
                successMessage += $" ({removedCount} item{(removedCount > 1 ? "s" : "")} completely used up and removed)";
            }
            successMessage += ".";
            
            TempData["SuccessMessage"] = successMessage;
            return RedirectToPage("/MealPlan/Details", new { id = planId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finishing meal {MealId}", mealId);
            TempData["ErrorMessage"] = "An error occurred while finishing the meal. Please try again.";
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
