using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IRecipeRepository : IRepository<Recipe>
{
    Task<List<Recipe>> SearchAsync(string searchTerm);
    Task<Recipe?> GetByIdWithIngredientsAsync(string id);
    Task<List<Recipe>> GetAllWithIngredientsAsync();
}
