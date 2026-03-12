using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IIngredientRepository : IRepository<Ingredient>
{
    Task<List<Ingredient>> SearchAsync(string searchTerm);
    Task<Ingredient?> GetByIdWithAllergiesAsync(string id);
}
