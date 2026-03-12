using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class MealTrackingService : IMealTrackingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MealTrackingService> _logger;

    public MealTrackingService(
        IUnitOfWork unitOfWork,
        ILogger<MealTrackingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<MealDto>> GetActiveMealsAsync(string userId)
    {
        _logger.LogInformation("Getting active meals for user {UserId}", userId);

        var now = DateTime.UtcNow.Date;
        
        // Get active meal plans with their meals
        var mealPlans = await _unitOfWork.MealPlans.GetAllQueryable()
            .Where(mp => mp.AccountId == userId && mp.IsActive)
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
                    .ThenInclude(mr => mr.Recipe)
            .ToListAsync();

        // Extract today's unfinished meals
        var meals = mealPlans
            .SelectMany(mp => mp.Meals)
            .Where(m => m.ServerDate.Date == now && !m.MealFinished)
            .OrderBy(m => m.MealTypeId)
            .ToList();

        var mealDtos = meals.Select(m => new MealDto
        {
            Id = m.Id,
            MealTypeId = m.MealTypeId,
            Status = m.MealFinished ? "Finished" : "Pending",
            Date = m.ServerDate,
            RecipeIds = m.MealRecipes.Select(mr => mr.RecipeId).ToList(),
            Recipes = m.MealRecipes.Select(mr => new RecipeDto
            {
                Id = mr.Recipe.Id,
                RecipeName = mr.Recipe.RecipeName,
                Instructions = mr.Recipe.Instructions
            }).ToList()
        }).ToList();

        return mealDtos;
    }

    public async Task<MealStatusDto> GetMealStatusAsync(string mealId)
    {
        _logger.LogInformation("Getting status for meal {MealId}", mealId);

        // Find meal through meal plans
        var mealPlans = await _unitOfWork.MealPlans.GetAllQueryable()
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
                    .ThenInclude(mr => mr.Recipe)
            .Where(mp => mp.Meals.Any(m => m.Id == mealId))
            .ToListAsync();

        var meal = mealPlans
            .SelectMany(mp => mp.Meals)
            .FirstOrDefault(m => m.Id == mealId);

        if (meal == null)
        {
            throw new KeyNotFoundException($"Meal {mealId} not found");
        }

        return new MealStatusDto
        {
            Id = meal.Id,
            MealTypeId = meal.MealTypeId,
            Status = meal.MealFinished ? "Finished" : "Pending",
            Date = meal.ServerDate,
            CompletedAt = meal.MealFinished ? meal.ServerDate : null,
            Recipes = meal.MealRecipes.Select(mr => new RecipeDto
            {
                Id = mr.Recipe.Id,
                RecipeName = mr.Recipe.RecipeName,
                Instructions = mr.Recipe.Instructions
            }).ToList()
        };
    }

    public async Task<MealFinishCheckDto> CheckMealIngredientsAsync(string mealPlanId, string mealId, string userId)
    {
        _logger.LogInformation("Checking ingredients for meal {MealId} in plan {MealPlanId}", mealId, mealPlanId);

        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .Where(mp => mp.Id == mealPlanId && mp.AccountId == userId)
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
                    .ThenInclude(mr => mr.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync();

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);

        if (meal == null)
        {
            throw new KeyNotFoundException($"Meal {mealId} not found in meal plan {mealPlanId}");
        }

        // Calculate required ingredients
        var requiredIngredients = new Dictionary<string, (string Name, string Unit, decimal Amount)>();
        
        foreach (var mealRecipe in meal.MealRecipes)
        {
            if (mealRecipe.Recipe?.RecipeIngredients == null) continue;

            foreach (var recipeIngredient in mealRecipe.Recipe.RecipeIngredients)
            {
                var ingredientId = recipeIngredient.IngredientId;
                if (requiredIngredients.ContainsKey(ingredientId))
                {
                    var existing = requiredIngredients[ingredientId];
                    requiredIngredients[ingredientId] = (existing.Name, existing.Unit, existing.Amount + recipeIngredient.Amount);
                }
                else
                {
                    requiredIngredients[ingredientId] = (
                        recipeIngredient.Ingredient.Name,
                        recipeIngredient.Ingredient.Unit,
                        recipeIngredient.Amount
                    );
                }
            }
        }

        // Check fridge availability
        var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);
        var availableIngredients = fridgeItems
            .Where(fi => fi.ExpiryDate >= DateTime.UtcNow)
            .GroupBy(fi => fi.IngredientId)
            .ToDictionary(g => g.Key, g => g.Sum(fi => fi.CurrentAmount));

        // Build ingredient check list
        var ingredientChecks = new List<MealIngredientCheckDto>();
        
        foreach (var required in requiredIngredients)
        {
            var ingredientId = required.Key;
            var (name, unit, requiredAmount) = required.Value;
            var availableAmount = availableIngredients.ContainsKey(ingredientId) ? availableIngredients[ingredientId] : 0;
            var missingAmount = Math.Max(0, requiredAmount - availableAmount);

            ingredientChecks.Add(new MealIngredientCheckDto
            {
                IngredientId = ingredientId,
                IngredientName = name,
                Unit = unit,
                RequiredAmount = requiredAmount,
                AvailableAmount = availableAmount,
                MissingAmount = missingAmount,
                IsAvailable = availableAmount >= requiredAmount
            });
        }

        var availableCount = ingredientChecks.Count(i => i.IsAvailable);
        var missingCount = ingredientChecks.Count(i => !i.IsAvailable);

        return new MealFinishCheckDto
        {
            MealId = mealId,
            Ingredients = ingredientChecks.OrderBy(i => i.IsAvailable).ThenBy(i => i.IngredientName).ToList(),
            CanFinish = true, // Always allow finishing, even with missing ingredients
            TotalIngredients = ingredientChecks.Count,
            AvailableIngredients = availableCount,
            MissingIngredients = missingCount
        };
    }

    public async Task MarkMealAsFinishedAsync(string mealPlanId, string mealId, string userId)
    {
        _logger.LogInformation("Marking meal {MealId} as finished for user {UserId}", mealId, userId);

        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .Where(mp => mp.Id == mealPlanId && mp.AccountId == userId)
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
                    .ThenInclude(mr => mr.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
            .FirstOrDefaultAsync();

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);

        if (meal == null)
        {
            throw new KeyNotFoundException($"Meal {mealId} not found in meal plan {mealPlanId}");
        }

        if (meal.MealFinished)
        {
            throw new InvalidOperationException("Meal is already marked as finished");
        }

        // Calculate required ingredients for this meal
        var requiredIngredients = new List<IngredientQuantityDto>();
        
        foreach (var mealRecipe in meal.MealRecipes)
        {
            if (mealRecipe.Recipe?.RecipeIngredients == null) continue;

            foreach (var recipeIngredient in mealRecipe.Recipe.RecipeIngredients)
            {
                var existing = requiredIngredients.FirstOrDefault(i => i.IngredientId == recipeIngredient.IngredientId);
                if (existing != null)
                {
                    existing.Quantity += recipeIngredient.Amount;
                }
                else
                {
                    requiredIngredients.Add(new IngredientQuantityDto
                    {
                        IngredientId = recipeIngredient.IngredientId,
                        Quantity = recipeIngredient.Amount
                    });
                }
            }
        }

        // Deduct ingredients from fridge
        if (requiredIngredients.Any())
        {
            try
            {
                await DeductIngredientsFromFridgeAsync(userId, requiredIngredients);
                _logger.LogInformation("Successfully deducted {Count} ingredients from fridge for meal {MealId}", 
                    requiredIngredients.Count, mealId);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to deduct ingredients from fridge for meal {MealId}", mealId);
                // Continue marking meal as finished even if ingredient deduction fails
                // This allows users to mark meals as finished even if they don't have all ingredients in fridge
            }
        }

        meal.MealFinished = true;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully marked meal {MealId} as finished", mealId);
    }

    private async Task DeductIngredientsFromFridgeAsync(string userId, List<IngredientQuantityDto> ingredients)
    {
        foreach (var ingredient in ingredients)
        {
            var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);
            var userIngredients = fridgeItems
                .Where(fi => fi.IngredientId == ingredient.IngredientId && fi.ExpiryDate >= DateTime.UtcNow)
                .OrderBy(fi => fi.ExpiryDate) // Use oldest items first (FIFO)
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
                _logger.LogWarning("Insufficient quantity for ingredient {IngredientId}. Missing: {Amount}", 
                    ingredient.IngredientId, remainingToDeduct);
                throw new InvalidOperationException($"Insufficient quantity for ingredient {ingredient.IngredientId}");
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkMealAsUnfinishedAsync(string mealPlanId, string mealId, string userId, UnfinishMealDto dto)
    {
        _logger.LogInformation("Marking meal {MealId} as unfinished for user {UserId}", mealId, userId);

        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .Where(mp => mp.Id == mealPlanId && mp.AccountId == userId)
            .Include(mp => mp.Meals)
            .FirstOrDefaultAsync();

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);

        if (meal == null)
        {
            throw new KeyNotFoundException($"Meal {mealId} not found in meal plan {mealPlanId}");
        }

        if (!meal.MealFinished)
        {
            throw new InvalidOperationException("Meal is not marked as finished");
        }

        // Return ingredients to fridge with custom amounts and expiry dates
        if (dto.Ingredients.Any())
        {
            await ReturnIngredientsToFridgeAsync(userId, dto.Ingredients);
            _logger.LogInformation("Successfully returned {Count} ingredients to fridge for meal {MealId}", 
                dto.Ingredients.Count, mealId);
        }

        meal.MealFinished = false;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully marked meal {MealId} as unfinished", mealId);
    }

    public async Task<MealUnfinishCheckDto> CheckMealUnfinishAsync(string mealPlanId, string mealId, string userId)
    {
        _logger.LogInformation("Checking ingredients to return for meal {MealId} in plan {MealPlanId}", mealId, mealPlanId);

        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .Where(mp => mp.Id == mealPlanId && mp.AccountId == userId)
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
                    .ThenInclude(mr => mr.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync();

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);

        if (meal == null)
        {
            throw new KeyNotFoundException($"Meal {mealId} not found in meal plan {mealPlanId}");
        }

        if (!meal.MealFinished)
        {
            throw new InvalidOperationException("Meal is not marked as finished");
        }

        // Calculate ingredients that were deducted
        var ingredientsToReturn = new List<IngredientReturnDto>();
        var defaultExpiryDate = DateTime.UtcNow.AddDays(7);
        
        foreach (var mealRecipe in meal.MealRecipes)
        {
            if (mealRecipe.Recipe?.RecipeIngredients == null) continue;

            foreach (var recipeIngredient in mealRecipe.Recipe.RecipeIngredients)
            {
                var existing = ingredientsToReturn.FirstOrDefault(i => i.IngredientId == recipeIngredient.IngredientId);
                if (existing != null)
                {
                    existing.Amount += recipeIngredient.Amount;
                }
                else
                {
                    ingredientsToReturn.Add(new IngredientReturnDto
                    {
                        IngredientId = recipeIngredient.IngredientId,
                        IngredientName = recipeIngredient.Ingredient.Name,
                        Unit = recipeIngredient.Ingredient.Unit,
                        Amount = recipeIngredient.Amount,
                        ExpiryDate = defaultExpiryDate
                    });
                }
            }
        }

        return new MealUnfinishCheckDto
        {
            MealId = mealId,
            Ingredients = ingredientsToReturn.OrderBy(i => i.IngredientName).ToList(),
            TotalIngredients = ingredientsToReturn.Count
        };
    }

    private async Task ReturnIngredientsToFridgeAsync(string userId, List<IngredientReturnDto> ingredients)
    {
        // Get or create fridge for user
        var fridge = await _unitOfWork.Fridges.GetByAccountIdAsync(userId);
        if (fridge == null)
        {
            fridge = new Domain.Entities.Fridge
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = userId,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Fridges.AddAsync(fridge);
        }

        foreach (var ingredient in ingredients)
        {
            // Add ingredient back to fridge with custom amount and expiry date
            var fridgeItem = new Domain.Entities.FridgeItem
            {
                Id = Guid.NewGuid().ToString(),
                FridgeId = fridge.Id,
                AccountId = userId,
                IngredientId = ingredient.IngredientId,
                CurrentAmount = ingredient.Amount,
                ExpiryDate = ingredient.ExpiryDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.FridgeItems.AddAsync(fridgeItem);
            _logger.LogInformation("Returned {Amount} {Unit} of {Name} to fridge with expiry {ExpiryDate}", 
                ingredient.Amount, ingredient.Unit, ingredient.IngredientName, ingredient.ExpiryDate.ToShortDateString());
        }

        fridge.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateExpiredMealsAsync()
    {
        _logger.LogInformation("Updating expired meals");

        var now = DateTime.UtcNow.Date;
        
        var mealPlans = await _unitOfWork.MealPlans.GetAllQueryable()
            .Include(mp => mp.Meals)
            .ToListAsync();

        var expiredMeals = mealPlans
            .SelectMany(mp => mp.Meals)
            .Where(m => m.ServerDate.Date < now && !m.MealFinished)
            .ToList();

        foreach (var meal in expiredMeals)
        {
            // Mark as expired or handle as needed
            // For now, we'll just log them
            _logger.LogInformation("Meal {MealId} expired on {Date}", meal.Id, meal.ServerDate);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<MealPlanProgressDto> GetMealPlanProgressAsync(string mealPlanId)
    {
        _logger.LogInformation("Getting progress for meal plan {MealPlanId}", mealPlanId);

        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .Include(mp => mp.Meals)
            .FirstOrDefaultAsync(mp => mp.Id == mealPlanId);

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        var now = DateTime.UtcNow.Date;
        var totalMeals = mealPlan.Meals.Count;
        var finishedMeals = mealPlan.Meals.Count(m => m.MealFinished);
        var expiredMeals = mealPlan.Meals.Count(m => m.ServerDate.Date < now && !m.MealFinished);
        var pendingMeals = mealPlan.Meals.Count(m => m.ServerDate.Date >= now && !m.MealFinished);

        var completionPercentage = totalMeals > 0 
            ? (decimal)finishedMeals / totalMeals * 100 
            : 0;

        return new MealPlanProgressDto
        {
            MealPlanId = mealPlan.Id,
            MealPlanName = mealPlan.PlanName,
            TotalMeals = totalMeals,
            FinishedMeals = finishedMeals,
            ExpiredMeals = expiredMeals,
            PendingMeals = pendingMeals,
            IsCompleted = finishedMeals == totalMeals && totalMeals > 0,
            CompletionPercentage = Math.Round(completionPercentage, 2)
        };
    }
}
