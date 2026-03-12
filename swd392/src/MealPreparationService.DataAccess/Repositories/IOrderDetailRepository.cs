using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for OrderDetail entity operations.
/// </summary>
public interface IOrderDetailRepository : IRepository<OrderDetail>
{
    /// <summary>
    /// Gets all order details for a specific order.
    /// </summary>
    Task<List<OrderDetail>> GetByOrderIdAsync(string orderId);
    
    /// <summary>
    /// Gets all order details for a specific menu meal.
    /// </summary>
    Task<List<OrderDetail>> GetByMenuMealIdAsync(string menuMealId);
}
