using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class IngredientAllergyRepository : Repository<IngredientAllergy>, IIngredientAllergyRepository
{
    public IngredientAllergyRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<IngredientAllergy>> GetByIngredientIdAsync(string ingredientId)
    {
        return await _context.IngredientAllergies
            .Where(ia => ia.IngredientId == ingredientId)
            .ToListAsync();
    }
}
