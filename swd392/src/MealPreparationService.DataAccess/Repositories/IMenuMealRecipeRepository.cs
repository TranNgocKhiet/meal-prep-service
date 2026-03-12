using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for MenuMealRecipe entity operations.
/// Note: MenuMealRecipe uses a composite key (MenuMealId, RecipeId) and does not inherit from BaseEntity.
/// </summary>
public interface IMenuMealRecipeRepository
{
    /// <summary>
    /// Gets a MenuMealRecipe by its composite key, including related MenuMeal and Recipe entities.
    /// </summary>
    Task<MenuMealRecipe?> GetByIdAsync(string menuMealId, string recipeId);
    
    /// <summary>
    /// Gets all MenuMealRecipe records for a specific menu meal.
    /// </summary>
    Task<List<MenuMealRecipe>> GetByMenuMealIdAsync(string menuMealId);
    
    /// <summary>
    /// Gets all MenuMealRecipe records for a specific recipe.
    /// </summary>
    Task<List<MenuMealRecipe>> GetByRecipeIdAsync(string recipeId);
    
    /// <summary>
    /// Gets all MenuMealRecipe records.
    /// </summary>
    Task<List<MenuMealRecipe>> GetAllAsync();
    
    /// <summary>
    /// Adds a new MenuMealRecipe record.
    /// </summary>
    Task<MenuMealRecipe> AddAsync(MenuMealRecipe entity);
    
    /// <summary>
    /// Updates an existing MenuMealRecipe record.
    /// </summary>
    Task<MenuMealRecipe> UpdateAsync(MenuMealRecipe entity);
    
    /// <summary>
    /// Deletes a MenuMealRecipe record by its composite key.
    /// </summary>
    Task DeleteAsync(string menuMealId, string recipeId);
    
    /// <summary>
    /// Checks if a MenuMealRecipe record exists by its composite key.
    /// </summary>
    Task<bool> ExistsAsync(string menuMealId, string recipeId);
    
    /// <summary>
    /// Gets the total count of MenuMealRecipe records.
    /// </summary>
    Task<int> CountAsync();
}
