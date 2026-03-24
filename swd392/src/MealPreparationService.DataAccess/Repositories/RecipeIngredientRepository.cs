using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class RecipeIngredientRepository : Repository<RecipeIngredient>, IRecipeIngredientRepository
{
    public RecipeIngredientRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<RecipeIngredient>> GetByRecipeIdAsync(string recipeId)
    {
        return await _context.RecipeIngredients
            .Include(ri => ri.Ingredient)
            .Where(ri => ri.RecipeId == recipeId)
            .ToListAsync();
    }
}
