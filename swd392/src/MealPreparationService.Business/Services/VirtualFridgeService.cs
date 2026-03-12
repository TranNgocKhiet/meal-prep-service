using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class VirtualFridgeService : IVirtualFridgeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VirtualFridgeService> _logger;

    public VirtualFridgeService(
        IUnitOfWork unitOfWork,
        ILogger<VirtualFridgeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<FridgeItemDto> AddItemAsync(AddFridgeItemDto dto, string userId)
    {
        _logger.LogInformation("Adding item to fridge for user {UserId}", userId);

        // Get or create fridge for user
        var fridge = await _unitOfWork.Fridges.GetByAccountIdAsync(userId);
        if (fridge == null)
        {
            fridge = new Fridge
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Fridges.AddAsync(fridge);
        }

        // Get ingredient to get its unit
        var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(dto.IngredientId);
        if (ingredient == null)
        {
            throw new ArgumentException("Ingredient not found");
        }

        // Create fridge item
        var fridgeItem = new FridgeItem
        {
            Id = Guid.NewGuid().ToString(),
            FridgeId = fridge.Id,
            AccountId = userId,
            IngredientId = dto.IngredientId,
            CurrentAmount = dto.Quantity,
            ExpiryDate = dto.ExpiryDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.FridgeItems.AddAsync(fridgeItem);
        fridge.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        // Load ingredient for response
        fridgeItem.Ingredient = ingredient;

        return MapToDto(fridgeItem);
    }

    public async Task<FridgeItemDto> UpdateItemAsync(string itemId, UpdateFridgeItemDto dto)
    {
        _logger.LogInformation("Updating fridge item {ItemId}", itemId);

        var fridgeItem = await _unitOfWork.FridgeItems.GetByIdWithIngredientAsync(itemId);
        if (fridgeItem == null)
        {
            throw new ArgumentException("Fridge item not found");
        }

        if (dto.Quantity.HasValue)
        {
            fridgeItem.CurrentAmount = dto.Quantity.Value;
        }

        if (dto.ExpiryDate.HasValue)
        {
            fridgeItem.ExpiryDate = dto.ExpiryDate.Value;
        }

        fridgeItem.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(fridgeItem);
    }

    public async Task DeleteItemAsync(string itemId)
    {
        _logger.LogInformation("Deleting fridge item {ItemId}", itemId);

        var fridgeItem = await _unitOfWork.FridgeItems.GetByIdAsync(itemId);
        if (fridgeItem == null)
        {
            throw new ArgumentException("Fridge item not found");
        }

        await _unitOfWork.FridgeItems.DeleteAsync(itemId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<FridgeItemDto>> GetUserFridgeItemsAsync(string userId, bool includeExpired = true)
    {
        _logger.LogInformation("Getting fridge items for user {UserId}", userId);

        var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);

        if (!includeExpired)
        {
            fridgeItems = fridgeItems.Where(fi => fi.ExpiryDate >= DateTime.UtcNow).ToList();
        }

        return fridgeItems.Select(MapToDto).ToList();
    }

    public async Task<bool> HasSufficientQuantityAsync(string userId, string ingredientId, decimal quantity)
    {
        var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);
        var totalAmount = fridgeItems
            .Where(fi => fi.IngredientId == ingredientId && fi.ExpiryDate >= DateTime.UtcNow)
            .Sum(fi => fi.CurrentAmount);

        return totalAmount >= quantity;
    }

    public async Task DeductIngredientsAsync(string userId, List<IngredientQuantityDto> ingredients)
    {
        _logger.LogInformation("Deducting ingredients for user {UserId}", userId);

        foreach (var ingredient in ingredients)
        {
            var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);
            var userIngredients = fridgeItems
                .Where(fi => fi.IngredientId == ingredient.IngredientId && fi.ExpiryDate >= DateTime.UtcNow)
                .OrderBy(fi => fi.ExpiryDate)
                .ToList();

            var remainingToDeduct = ingredient.Quantity;

            foreach (var item in userIngredients)
            {
                if (remainingToDeduct <= 0) break;

                if (item.CurrentAmount <= remainingToDeduct)
                {
                    remainingToDeduct -= item.CurrentAmount;
                    await _unitOfWork.FridgeItems.DeleteAsync(item.Id);
                }
                else
                {
                    item.CurrentAmount -= remainingToDeduct;
                    item.UpdatedAt = DateTime.UtcNow;
                    remainingToDeduct = 0;
                }
            }

            if (remainingToDeduct > 0)
            {
                throw new InvalidOperationException($"Insufficient quantity for ingredient {ingredient.IngredientId}");
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<FridgeItemDto>> GetExpiringItemsAsync(string userId, int daysThreshold)
    {
        _logger.LogInformation("Getting expiring items for user {UserId}", userId);

        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
        var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);

        var expiringItems = fridgeItems
            .Where(fi => fi.ExpiryDate <= thresholdDate && fi.ExpiryDate >= DateTime.UtcNow)
            .ToList();

        return expiringItems.Select(MapToDto).ToList();
    }

    public async Task<GroceryListDto> GenerateGroceryListAsync(string userId)
    {
        _logger.LogInformation("Generating grocery list for user {UserId}", userId);

        // Get active meal plans
        var activeMealPlans = await _unitOfWork.MealPlans.GetByUserIdAndStatusAsync(userId, 1);
        if (!activeMealPlans.Any())
        {
            _logger.LogWarning("No active meal plan found for user {UserId}", userId);
            return new GroceryListDto { Items = new List<GroceryListItemDto>(), TotalEstimatedCost = 0, TotalItems = 0 };
        }

        var activeMealPlan = activeMealPlans.First();
        
        // Get meal plan with details
        var mealPlanWithDetails = await _unitOfWork.MealPlans.GetByIdWithDetailsAsync(activeMealPlan.Id);
        if (mealPlanWithDetails == null || !mealPlanWithDetails.Meals.Any())
        {
            _logger.LogWarning("No meals found in active meal plan for user {UserId}", userId);
            return new GroceryListDto { Items = new List<GroceryListItemDto>(), TotalEstimatedCost = 0, TotalItems = 0 };
        }

        // Get unfinished meals
        var unfinishedMeals = mealPlanWithDetails.Meals.Where(m => !m.MealFinished).ToList();
        _logger.LogInformation("Found {Count} unfinished meals", unfinishedMeals.Count);

        if (!unfinishedMeals.Any())
        {
            _logger.LogInformation("No unfinished meals found for user {UserId}", userId);
            return new GroceryListDto { Items = new List<GroceryListItemDto>(), TotalEstimatedCost = 0, TotalItems = 0 };
        }

        // Calculate required ingredients from unfinished meals
        var requiredIngredients = new Dictionary<string, decimal>();
        
        foreach (var meal in unfinishedMeals)
        {
            var mealWithRecipes = await _unitOfWork.MealPlans.GetMealByIdAsync(meal.Id);
            if (mealWithRecipes?.MealRecipes == null) continue;

            foreach (var mealRecipe in mealWithRecipes.MealRecipes)
            {
                var recipe = await _unitOfWork.Recipes.GetByIdWithIngredientsAsync(mealRecipe.RecipeId);
                if (recipe?.RecipeIngredients == null) continue;

                foreach (var recipeIngredient in recipe.RecipeIngredients)
                {
                    if (requiredIngredients.ContainsKey(recipeIngredient.IngredientId))
                    {
                        requiredIngredients[recipeIngredient.IngredientId] += recipeIngredient.Amount;
                    }
                    else
                    {
                        requiredIngredients[recipeIngredient.IngredientId] = recipeIngredient.Amount;
                    }
                }
            }
        }

        // Get current fridge items
        var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);
        var currentIngredients = fridgeItems
            .Where(fi => fi.ExpiryDate >= DateTime.UtcNow)
            .GroupBy(fi => fi.IngredientId)
            .ToDictionary(g => g.Key, g => g.Sum(fi => fi.CurrentAmount));

        // Calculate missing ingredients
        var groceryListItems = new List<GroceryListItemDto>();
        
        foreach (var required in requiredIngredients)
        {
            var ingredientId = required.Key;
            var requiredAmount = required.Value;
            var currentAmount = currentIngredients.ContainsKey(ingredientId) ? currentIngredients[ingredientId] : 0;
            var missingAmount = requiredAmount - currentAmount;

            if (missingAmount > 0)
            {
                var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(ingredientId);
                if (ingredient != null)
                {
                    groceryListItems.Add(new GroceryListItemDto
                    {
                        IngredientId = ingredient.Id,
                        IngredientName = ingredient.Name,
                        Unit = ingredient.Unit,
                        RequiredQuantity = requiredAmount,
                        CurrentQuantity = currentAmount,
                        MissingQuantity = missingAmount,
                        PricePerUnit = 0, // Price not available in current schema
                        EstimatedCost = 0, // Price not available in current schema
                        IsSelected = true
                    });
                }
            }
        }

        var totalCost = groceryListItems.Sum(item => item.EstimatedCost);

        _logger.LogInformation("Generated grocery list with {Count} items for user {UserId}", groceryListItems.Count, userId);

        return new GroceryListDto
        {
            Items = groceryListItems,
            TotalEstimatedCost = totalCost,
            TotalItems = groceryListItems.Count
        };
    }

    public async Task<List<FridgeItemDto>> PurchaseGroceryItemsAsync(string userId, PurchaseGroceryListDto dto)
    {
        _logger.LogInformation("Purchasing {Count} grocery items for user {UserId}", dto.Items.Count, userId);

        var addedItems = new List<FridgeItemDto>();

        foreach (var item in dto.Items)
        {
            var addDto = new AddFridgeItemDto
            {
                IngredientId = item.IngredientId,
                Quantity = item.Quantity,
                ExpiryDate = item.ExpiryDate
            };

            var addedItem = await AddItemAsync(addDto, userId);
            addedItems.Add(addedItem);
        }

        _logger.LogInformation("Successfully purchased {Count} items for user {UserId}", addedItems.Count, userId);

        return addedItems;
    }

    private FridgeItemDto MapToDto(FridgeItem item)
    {
        var now = DateTime.UtcNow;
        var daysUntilExpiry = (item.ExpiryDate.Date - now.Date).Days;

        return new FridgeItemDto
        {
            Id = item.Id,
            Ingredient = new IngredientDto
            {
                Id = item.Ingredient.Id,
                Name = item.Ingredient.Name,
                Category = "Ingredient",
                Unit = item.Ingredient.Unit,
                ImageUrl = item.Ingredient.ImageUrl ?? string.Empty
            },
            Quantity = item.CurrentAmount,
            Unit = item.Ingredient.Unit,
            ExpiryDate = item.ExpiryDate,
            IsExpired = item.ExpiryDate < now,
            DaysUntilExpiry = daysUntilExpiry,
            AddedAt = item.CreatedAt
        };
    }
}
