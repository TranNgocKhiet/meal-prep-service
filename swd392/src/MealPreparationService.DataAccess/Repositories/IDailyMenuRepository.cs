using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for DailyMenu entity operations.
/// </summary>
public interface IDailyMenuRepository : IRepository<DailyMenu>
{
    /// <summary>
    /// Gets a DailyMenu by its menu date.
    /// </summary>
    Task<DailyMenu?> GetByDateAsync(DateTime menuDate);
    
    /// <summary>
    /// Gets all DailyMenu records within a date range.
    /// </summary>
    Task<List<DailyMenu>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Gets all DailyMenu records filtered by status.
    /// </summary>
    Task<List<DailyMenu>> GetByStatusAsync(int statusId);
}
