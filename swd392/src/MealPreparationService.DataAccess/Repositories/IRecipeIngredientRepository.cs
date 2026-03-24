using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IRecipeIngredientRepository : IRepository<RecipeIngredient>
{
    Task<List<RecipeIngredient>> GetByRecipeIdAsync(string recipeId);
}
