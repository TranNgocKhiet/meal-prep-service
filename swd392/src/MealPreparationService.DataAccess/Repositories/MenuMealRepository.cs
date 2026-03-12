using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for MenuMeal entity operations.
/// Handles CRUD operations for MenuMeal entities with related data.
/// </summary>
public class MenuMealRepository : Repository<MenuMeal>, IMenuMealRepository
{
    public MenuMealRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a MenuMeal by ID, including related DailyMenu and MenuMealRecipes.
    /// </summary>
    public override async Task<MenuMeal?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(mm => mm.Menu)
            .Include(mm => mm.MenuMealRecipes)
                .ThenInclude(mmr => mmr.Recipe)
            .FirstOrDefaultAsync(mm => mm.Id == id);
    }

    /// <summary>
    /// Gets all MenuMeal records for a specific menu (DailyMenu).
    /// </summary>
    public async Task<List<MenuMeal>> GetByMenuIdAsync(string menuId)
    {
        return await _dbSet
            .Include(mm => mm.Menu)
            .Include(mm => mm.MenuMealRecipes)
                .ThenInclude(mmr => mmr.Recipe)
            .Where(mm => mm.MenuId == menuId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all MenuMeal records for a specific menu filtered by meal type.
    /// </summary>
    public async Task<List<MenuMeal>> GetByMealTypeAsync(string menuId, int mealTypeId)
    {
        return await _dbSet
            .Include(mm => mm.Menu)
            .Include(mm => mm.MealType)
            .Include(mm => mm.MenuMealRecipes)
                .ThenInclude(mmr => mmr.Recipe)
            .Where(mm => mm.MenuId == menuId && mm.MealTypeId == mealTypeId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all available MenuMeal records for a specific menu (where AvailableQuantity > 0).
    /// </summary>
    public async Task<List<MenuMeal>> GetAvailableMenuMealsAsync(string menuId)
    {
        return await _dbSet
            .Include(mm => mm.Menu)
            .Include(mm => mm.MenuMealRecipes)
                .ThenInclude(mmr => mmr.Recipe)
            .Where(mm => mm.MenuId == menuId && mm.AvailableQuantity > 0)
            .ToListAsync();
    }
}
