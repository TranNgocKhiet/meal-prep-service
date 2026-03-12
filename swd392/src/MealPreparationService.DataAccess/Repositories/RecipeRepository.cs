using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class RecipeRepository : Repository<Recipe>, IRecipeRepository
{
    public RecipeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Recipe>> SearchAsync(string searchTerm)
    {
        return await _dbSet
            .Where(r => r.RecipeName.Contains(searchTerm) || r.Instructions.Contains(searchTerm))
            .ToListAsync();
    }

    public async Task<Recipe?> GetByIdWithIngredientsAsync(string id)
    {
        return await _dbSet
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                    .ThenInclude(i => i.IngredientAllergies)
                        .ThenInclude(ia => ia.Allergy)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Recipe>> GetAllWithIngredientsAsync()
    {
        return await _dbSet
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
            .ToListAsync();
    }

}
