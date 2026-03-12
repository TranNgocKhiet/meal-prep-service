using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for OrderDetail entity operations.
/// </summary>
public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
{
    public OrderDetailRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets an order detail by ID with related entities (Order, MenuMeal).
    /// </summary>
    public override async Task<OrderDetail?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(od => od.Order)
            .Include(od => od.MenuMeal)
            .FirstOrDefaultAsync(od => od.Id == id);
    }

    /// <summary>
    /// Gets all order details for a specific order.
    /// </summary>
    public async Task<List<OrderDetail>> GetByOrderIdAsync(string orderId)
    {
        return await _dbSet
            .Include(od => od.MenuMeal)
            .Where(od => od.OrderId == orderId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all order details for a specific menu meal.
    /// </summary>
    public async Task<List<OrderDetail>> GetByMenuMealIdAsync(string menuMealId)
    {
        return await _dbSet
            .Include(od => od.Order)
            .Where(od => od.MenuMealId == menuMealId)
            .ToListAsync();
    }
}
