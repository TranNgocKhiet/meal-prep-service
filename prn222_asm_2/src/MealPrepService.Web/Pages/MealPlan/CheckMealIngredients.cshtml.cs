using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class CheckMealIngredientsModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<CheckMealIngredientsModel> _logger;

    public CheckMealIngredientsModel(IMealPlanService mealPlanService, IFridgeService fridgeService, ILogger<CheckMealIngredientsModel> logger)
    {
        _mealPlanService = mealPlanService;
        _fridgeService = fridgeService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid mealId, Guid planId)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var mealPlan = await _mealPlanService.GetByIdAsync(planId);
            
            if (mealPlan == null || mealPlan.AccountId != accountId)
            {
                return new JsonResult(new { success = false, message = "Meal plan not found or access denied." });
            }

            var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);
            if (meal == null)
            {
                return new JsonResult(new { success = false, message = "Meal not found." });
            }

            var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            var fridgeInventory = fridgeItems
                .GroupBy(f => f.IngredientId)
                .ToDictionary(
                    g => g.Key, 
                    g => new { 
                        Items = g.ToList(), 
                        TotalAmount = g.Sum(f => f.CurrentAmount) 
                    });

            var requiredIngredients = new Dictionary<Guid, float>();
            var ingredientNames = new Dictionary<Guid, string>();
            var ingredientUnits = new Dictionary<Guid, string>();

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
                            ingredientNames[ingredient.IngredientId] = ingredient.IngredientName;
                            ingredientUnits[ingredient.IngredientId] = ingredient.Unit;
                        }
                    }
                }
            }

            var missingIngredients = new List<object>();
            var insufficientIngredients = new List<object>();
            var consumptionPlan = new List<object>();

            foreach (var required in requiredIngredients)
            {
                var ingredientId = required.Key;
                var requiredAmount = required.Value;
                var ingredientName = ingredientNames[ingredientId];
                var unit = ingredientUnits[ingredientId];

                if (!fridgeInventory.ContainsKey(ingredientId))
                {
                    missingIngredients.Add(new
                    {
                        name = ingredientName,
                        required = requiredAmount,
                        unit = unit
                    });
                }
                else
                {
                    var fridgeGroup = fridgeInventory[ingredientId];
                    if (fridgeGroup.TotalAmount < requiredAmount)
                    {
                        insufficientIngredients.Add(new
                        {
                            name = ingredientName,
                            required = requiredAmount,
                            available = fridgeGroup.TotalAmount,
                            unit = unit
                        });
                    }
                    else
                    {
                        var itemsToConsume = fridgeGroup.Items.OrderBy(f => f.ExpiryDate).ToList();
                        var remainingToConsume = requiredAmount;
                        
                        foreach (var item in itemsToConsume)
                        {
                            if (remainingToConsume <= 0) break;
                            
                            var amountToConsume = Math.Min(item.CurrentAmount, remainingToConsume);
                            var newAmount = item.CurrentAmount - amountToConsume;
                            
                            consumptionPlan.Add(new
                            {
                                fridgeItemId = item.Id,
                                ingredientName = ingredientName,
                                amount = amountToConsume,
                                newAmount = newAmount,
                                unit = unit,
                                expiryDate = item.ExpiryDate
                            });
                            
                            remainingToConsume -= amountToConsume;
                        }
                    }
                }
            }

            return new JsonResult(new
            {
                success = true,
                hasIssues = missingIngredients.Any() || insufficientIngredients.Any(),
                missingIngredients = missingIngredients,
                insufficientIngredients = insufficientIngredients,
                consumptionPlan = consumptionPlan
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking meal ingredients for meal {MealId}", mealId);
            return new JsonResult(new { success = false, message = "An error occurred while checking ingredients." });
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
