using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer")]
public class MarkMealFinishedModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<MarkMealFinishedModel> _logger;

    public MarkMealFinishedModel(IMealPlanService mealPlanService, IFridgeService fridgeService, ILogger<MarkMealFinishedModel> logger)
    {
        _mealPlanService = mealPlanService;
        _fridgeService = fridgeService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid mealId, Guid planId, bool finished)
    {
        try
        {
            var accountId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            
            // If unmarking a finished meal, restore ingredients to the fridge
            if (!finished)
            {
                await RestoreIngredientsToFridge(mealId, planId, accountId);
            }
            
            await _mealPlanService.MarkMealAsFinishedAsync(mealId, accountId, finished);
            
            TempData["SuccessMessage"] = finished ? "Meal marked as finished!" : "Meal marked as not finished and ingredients restored to fridge.";
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
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Business error marking meal as finished");
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/MealPlan/Details", new { id = planId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking meal as finished");
            TempData["ErrorMessage"] = "An error occurred while updating the meal status.";
            return RedirectToPage("/MealPlan/Details", new { id = planId });
        }
    }

    private async Task RestoreIngredientsToFridge(Guid mealId, Guid planId, Guid accountId)
    {
        try
        {
            var mealPlan = await _mealPlanService.GetByIdAsync(planId);
            if (mealPlan == null || mealPlan.AccountId != accountId)
            {
                throw new AuthorizationException("Access denied to this meal plan.");
            }

            var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);
            if (meal == null)
            {
                throw new NotFoundException("Meal not found.");
            }

            var ingredientsToRestore = new Dictionary<Guid, (string Name, float Amount, string Unit)>();
            foreach (var recipe in meal.Recipes)
            {
                if (recipe.Ingredients != null)
                {
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        if (ingredientsToRestore.ContainsKey(ingredient.IngredientId))
                        {
                            var existing = ingredientsToRestore[ingredient.IngredientId];
                            ingredientsToRestore[ingredient.IngredientId] = (
                                existing.Name,
                                existing.Amount + ingredient.Amount,
                                existing.Unit
                            );
                        }
                        else
                        {
                            ingredientsToRestore[ingredient.IngredientId] = (
                                ingredient.IngredientName,
                                ingredient.Amount,
                                ingredient.Unit
                            );
                        }
                    }
                }
            }

            var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            var fridgeInventory = fridgeItems
                .GroupBy(f => f.IngredientId)
                .ToDictionary(g => g.Key, g => g.First());

            int restoredCount = 0;
            int addedCount = 0;

            foreach (var ingredient in ingredientsToRestore)
            {
                var ingredientId = ingredient.Key;
                var (name, amount, unit) = ingredient.Value;

                if (fridgeInventory.ContainsKey(ingredientId))
                {
                    var fridgeItem = fridgeInventory[ingredientId];
                    var newAmount = fridgeItem.CurrentAmount + amount;
                    await _fridgeService.UpdateItemQuantityAsync(fridgeItem.Id, newAmount);
                    restoredCount++;
                }
                else
                {
                    await _fridgeService.AddItemAsync(new FridgeItemDto
                    {
                        AccountId = accountId,
                        IngredientId = ingredientId,
                        CurrentAmount = amount,
                        ExpiryDate = DateTime.UtcNow.Date.AddDays(7)
                    });
                    addedCount++;
                }
            }

            _logger.LogInformation(
                "Restored ingredients for meal {MealId}: {RestoredCount} updated, {AddedCount} added back to fridge for account {AccountId}",
                mealId, restoredCount, addedCount, accountId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring ingredients for meal {MealId}", mealId);
            throw;
        }
    }
}
