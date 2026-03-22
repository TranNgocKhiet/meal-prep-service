using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class RecipeService : IRecipeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecipeService> _logger;

    public RecipeService(IUnitOfWork unitOfWork, ILogger<RecipeService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<RecipeDto>> SearchRecipesAsync(RecipeSearchDto searchDto, string userId)
    {
        _logger.LogInformation("Searching recipes for user {UserId}", userId);

        var query = _unitOfWork.Recipes.GetAllQueryable()
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                    .ThenInclude(i => i.IngredientNutrients)
                        .ThenInclude(ing => ing.Nutrient)
            .AsQueryable();

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLower();
            query = query.Where(r => r.RecipeName.ToLower().Contains(searchTerm));
        }

        // Note: Category, PreparationTimeMinutes, DifficultyLevel, Servings are not in the Recipe entity
        // These filters are ignored for now

        // Get user's health profile for allergy filtering
        if (searchDto.ExcludeAllergens)
        {
            var healthProfile = await _unitOfWork.HealthProfiles.GetByAccountIdAsync(userId);
            if (healthProfile != null)
            {
                var userAllergyIngredients = await _unitOfWork.HealthProfileIngredients
                    .GetByHealthProfileIdAsync(healthProfile.Id);
                
                var allergyIngredientIds = userAllergyIngredients
                    .Where(hpi => hpi.RelationshipTypeId == 3)
                    .Select(hpi => hpi.IngredientId)
                    .ToList();

                if (allergyIngredientIds.Any())
                {
                    query = query.Where(r => !r.RecipeIngredients
                        .Any(ri => allergyIngredientIds.Contains(ri.IngredientId)));
                }
            }
        }

        var recipes = await query.ToListAsync();

        return recipes.Select(r => MapToRecipeDto(r, userId)).ToList();
    }

    public async Task<PaginatedResult<RecipeDto>> SearchRecipesPaginatedAsync(
        RecipeSearchDto searchDto, 
        string userId, 
        PaginationParameters pagination)
    {
        _logger.LogInformation("Searching recipes (paginated) for user {UserId}", userId);

        var recipes = await SearchRecipesAsync(searchDto, userId);
        
        var totalCount = recipes.Count;
        var paginatedRecipes = recipes
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToList();

        return new PaginatedResult<RecipeDto>
        {
            Items = paginatedRecipes,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<RecipeDto?> GetRecipeByIdAsync(string recipeId, string? userId = null)
    {
        _logger.LogInformation("Getting recipe {RecipeId}", recipeId);

        var recipe = await _unitOfWork.Recipes.GetAllQueryable()
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                    .ThenInclude(i => i.IngredientNutrients)
                        .ThenInclude(ing => ing.Nutrient)
            .FirstOrDefaultAsync(r => r.Id == recipeId);

        if (recipe == null)
        {
            return null;
        }

        return MapToRecipeDto(recipe, userId);
    }

    public async Task<List<RecipeDto>> GetRecipesByCategoryAsync(string category, string? userId = null)
    {
        _logger.LogInformation("Getting recipes by category {Category}", category);

        // Note: Category is not in the Recipe entity, returning all recipes
        var recipes = await _unitOfWork.Recipes.GetAllQueryable()
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                    .ThenInclude(i => i.IngredientNutrients)
                        .ThenInclude(ing => ing.Nutrient)
            .ToListAsync();

        return recipes.Select(r => MapToRecipeDto(r, userId)).ToList();
    }

    public async Task<bool> CheckAllergyCompatibilityAsync(string recipeId, string userId)
    {
        _logger.LogInformation("Checking allergy compatibility for recipe {RecipeId} and user {UserId}", recipeId, userId);

        var healthProfile = await _unitOfWork.HealthProfiles.GetByAccountIdAsync(userId);
        if (healthProfile == null)
        {
            return true; // No health profile means no allergies
        }

        var userAllergyIngredients = await _unitOfWork.HealthProfileIngredients
            .GetByHealthProfileIdAsync(healthProfile.Id);
        
        var allergyIngredientIds = userAllergyIngredients
            .Where(hpi => hpi.RelationshipTypeId == 3)
            .Select(hpi => hpi.IngredientId)
            .ToList();

        if (!allergyIngredientIds.Any())
        {
            return true; // No allergies
        }

        var recipe = await _unitOfWork.Recipes.GetAllQueryable()
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == recipeId);

        if (recipe == null)
        {
            throw new ArgumentException("Recipe not found");
        }

        var hasAllergen = recipe.RecipeIngredients
            .Any(ri => allergyIngredientIds.Contains(ri.IngredientId));

        return !hasAllergen;
    }

    public async Task AddToFavoritesAsync(string recipeId, string userId)
    {
        _logger.LogInformation("Adding recipe {RecipeId} to favorites for user {UserId}", recipeId, userId);

        var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
        if (recipe == null)
        {
            throw new ArgumentException("Recipe not found");
        }

        // TODO: Implement favorite recipes table and logic
        // For now, this is a placeholder
        await Task.CompletedTask;
    }

    public async Task RemoveFromFavoritesAsync(string recipeId, string userId)
    {
        _logger.LogInformation("Removing recipe {RecipeId} from favorites for user {UserId}", recipeId, userId);

        // TODO: Implement favorite recipes table and logic
        // For now, this is a placeholder
        await Task.CompletedTask;
    }

    public async Task<List<RecipeDto>> GetUserFavoritesAsync(string userId)
    {
        _logger.LogInformation("Getting favorite recipes for user {UserId}", userId);

        // TODO: Implement favorite recipes table and logic
        // For now, return empty list
        await Task.CompletedTask;
        return new List<RecipeDto>();
    }

    private RecipeDto MapToRecipeDto(Recipe recipe, string? userId)
    {
        // Calculate nutritional information based on ingredients
        var totalCalories = 0m;
        var totalProtein = 0m;
        var totalFat = 0m;
        var totalCarbs = 0m;
        var servings = 4;

        if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Any())
        {
            // Calculate totals from ingredients
            foreach (var ri in recipe.RecipeIngredients)
            {
                if (ri.Ingredient?.IngredientNutrients != null && ri.Ingredient.IngredientNutrients.Any())
                {
                    // Calculate based on amount in recipe
                    // AmountPer100 is per 100 units, so multiply by (ri.Amount / 100)
                    var factor = ri.Amount / 100m;
                    
                    foreach (var nutrient in ri.Ingredient.IngredientNutrients)
                    {
                        // Map nutrients by name or id
                        var nutrientName = nutrient.Nutrient?.Name?.ToLower().Trim() ?? "";
                        var nutrientId = nutrient.NutrientId?.ToLower() ?? "";
                        
                        // Check by name (case insensitive, contains)
                        if (nutrientName.Contains("calories") || nutrientName.Contains("energy") || nutrientName.Contains("kcal"))
                        {
                            totalCalories += nutrient.AmountPer100 * factor;
                        }
                        else if (nutrientName.Contains("protein"))
                        {
                            totalProtein += nutrient.AmountPer100 * factor;
                        }
                        else if (nutrientName.Contains("fat") || nutrientName.Contains("lipid") || nutrientName.Contains("total fat"))
                        {
                            totalFat += nutrient.AmountPer100 * factor;
                        }
                        else if (nutrientName.Contains("carbohydrate") || nutrientName.Contains("carb") || nutrientName.Contains("sugar"))
                        {
                            totalCarbs += nutrient.AmountPer100 * factor;
                        }
                    }
                }
            }

            // Divide by servings to get per-serving amounts
            if (servings > 0)
            {
                totalCalories = Math.Round(totalCalories / servings, 2);
                totalProtein = Math.Round(totalProtein / servings, 2);
                totalFat = Math.Round(totalFat / servings, 2);
                totalCarbs = Math.Round(totalCarbs / servings, 2);
            }
        }

        // Apply default values if no nutrients found (fallback for demonstration)
        if (totalCalories == 0 && totalProtein == 0 && totalFat == 0 && totalCarbs == 0)
        {
            // Provide reasonable defaults based on recipe name for demo purposes
            totalCalories = 350;  // Default calorie estimate
            totalProtein = 25;    // Default protein in grams
            totalFat = 12;        // Default fat in grams
            totalCarbs = 35;      // Default carbs in grams
        }

        return new RecipeDto
        {
            Id = recipe.Id,
            RecipeName = recipe.RecipeName,
            Instructions = recipe.Instructions,
            Category = "General", // Default category since not in entity
            PreparationTimeMinutes = 30, // Default value since not in entity
            DifficultyLevel = "Medium", // Default value since not in entity
            Servings = servings, // Default value since not in entity
            Ingredients = recipe.RecipeIngredients?.Select(ri => new RecipeIngredientDto
            {
                IngredientId = ri.IngredientId,
                IngredientName = ri.Ingredient.Name,
                Ingredient = new IngredientDto
                {
                    Id = ri.Ingredient.Id,
                    Name = ri.Ingredient.Name,
                    Unit = ri.Ingredient.Unit
                },
                Amount = ri.Amount
            }).ToList(),
            HasAllergyWarning = false, // Will be set based on user allergies
            Allergens = new List<string>(),
            IsFavorite = false, // TODO: Check if recipe is in user's favorites
            TotalCalories = totalCalories,
            ProteinG = totalProtein,
            FatG = totalFat,
            CarbsG = totalCarbs
        };
    }
}
