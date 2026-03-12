using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;

namespace MealPreparationService.Business.Services;

public interface IRecipeService
{
    Task<List<RecipeDto>> SearchRecipesAsync(RecipeSearchDto searchDto, string userId);
    Task<PaginatedResult<RecipeDto>> SearchRecipesPaginatedAsync(RecipeSearchDto searchDto, string userId, PaginationParameters pagination);
    Task<RecipeDto?> GetRecipeByIdAsync(string recipeId, string? userId = null);
    Task<List<RecipeDto>> GetRecipesByCategoryAsync(string category, string? userId = null);
    Task<bool> CheckAllergyCompatibilityAsync(string recipeId, string userId);
    Task AddToFavoritesAsync(string recipeId, string userId);
    Task RemoveFromFavoritesAsync(string recipeId, string userId);
    Task<List<RecipeDto>> GetUserFavoritesAsync(string userId);
}
