using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IIngredientAllergyRepository : IRepository<IngredientAllergy>
{
    Task<List<IngredientAllergy>> GetByIngredientIdAsync(string ingredientId);
}
