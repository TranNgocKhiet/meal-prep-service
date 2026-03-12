using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for MenuMeal entity operations.
/// </summary>
public interface IMenuMealRepository : IRepository<MenuMeal>
{
    /// <summary>
    /// Gets all MenuMeal records for a specific menu (DailyMenu).
    /// </summary>
    Task<List<MenuMeal>> GetByMenuIdAsync(string menuId);
    
    /// <summary>
    /// Gets all MenuMeal records for a specific menu filtered by meal type.
    /// </summary>
    Task<List<MenuMeal>> GetByMealTypeAsync(string menuId, int mealTypeId);
    
    /// <summary>
    /// Gets all available MenuMeal records for a specific menu (where AvailableQuantity > 0).
    /// </summary>
    Task<List<MenuMeal>> GetAvailableMenuMealsAsync(string menuId);
}
