using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Service interface for recipe management operations
    /// </summary>
    public interface IRecipeService
    {
        Task<RecipeDto> CreateRecipeAsync(CreateRecipeDto dto);
        Task<RecipeDto> GetByIdAsync(Guid recipeId);
        Task<RecipeDto> GetByIdWithIngredientsAsync(Guid recipeId);
        Task<IEnumerable<RecipeDto>> GetAllAsync();
        Task<IEnumerable<RecipeDto>> GetAllWithIngredientsAsync();
        Task<RecipeDto> UpdateRecipeAsync(Guid recipeId, UpdateRecipeDto dto);
        Task DeleteRecipeAsync(Guid recipeId);
        Task AddIngredientToRecipeAsync(Guid recipeId, RecipeIngredientDto ingredientDto);
        Task UpdateRecipeIngredientAsync(Guid recipeId, Guid ingredientId, float newAmount);
        Task RemoveIngredientFromRecipeAsync(Guid recipeId, Guid ingredientId);
        Task<IEnumerable<RecipeDto>> GetByIngredientsAsync(IEnumerable<Guid> ingredientIds);
        Task<IEnumerable<RecipeDto>> GetExcludingAllergensAsync(IEnumerable<Guid> allergyIds);
    }
}