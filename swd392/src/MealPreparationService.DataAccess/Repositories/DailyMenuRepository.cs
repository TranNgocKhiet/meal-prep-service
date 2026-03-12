using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for DailyMenu entity operations.
/// Handles CRUD operations for DailyMenu entities with related data.
/// </summary>
public class DailyMenuRepository : Repository<DailyMenu>, IDailyMenuRepository
{
    public DailyMenuRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a DailyMenu by ID, including related Status and MenuMeals.
    /// </summary>
    public override async Task<DailyMenu?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(dm => dm.Status)
            .Include(dm => dm.MenuMeals)
            .FirstOrDefaultAsync(dm => dm.Id == id);
    }

    /// <summary>
    /// Gets a DailyMenu by its menu date.
    /// </summary>
    public async Task<DailyMenu?> GetByDateAsync(DateTime menuDate)
    {
        return await _dbSet
            .Include(dm => dm.Status)
            .Include(dm => dm.MenuMeals)
            .FirstOrDefaultAsync(dm => dm.MenuDate.Date == menuDate.Date);
    }

    /// <summary>
    /// Gets all DailyMenu records within a date range.
    /// </summary>
    public async Task<List<DailyMenu>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(dm => dm.Status)
            .Include(dm => dm.MenuMeals)
            .Where(dm => dm.MenuDate.Date >= startDate.Date && dm.MenuDate.Date <= endDate.Date)
            .OrderBy(dm => dm.MenuDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all DailyMenu records filtered by status.
    /// </summary>
    public async Task<List<DailyMenu>> GetByStatusAsync(int statusId)
    {
        return await _dbSet
            .Include(dm => dm.Status)
            .Include(dm => dm.MenuMeals)
            .Where(dm => dm.StatusId == statusId)
            .OrderBy(dm => dm.MenuDate)
            .ToListAsync();
    }
}
