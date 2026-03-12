using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Order>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Include(o => o.Status)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetByStatusAsync(int statusId)
    {
        return await _dbSet
            .Include(o => o.Customer)
            .Include(o => o.Status)
            .Where(o => o.StatusId == statusId)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdWithDetailsAsync(string id)
    {
        return await _dbSet
            .Include(o => o.Status)
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public override async Task<Order?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(o => o.Status)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}
