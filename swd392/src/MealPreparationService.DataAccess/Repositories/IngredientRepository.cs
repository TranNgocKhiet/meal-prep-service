using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class IngredientRepository : Repository<Ingredient>, IIngredientRepository
{
    public IngredientRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Ingredient>> SearchAsync(string searchTerm)
    {
        return await _dbSet
            .Where(i => i.Name.Contains(searchTerm))
            .ToListAsync();
    }


    public async Task<Ingredient?> GetByIdWithAllergiesAsync(string id)
    {
        return await _dbSet
            .Include(i => i.IngredientAllergies)
                .ThenInclude(ia => ia.Allergy)
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}
