using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository implementation for Order entity
    /// </summary>
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(MealPrepDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Order>> GetByAccountIdAsync(Guid accountId)
        {
            return await _dbSet
                .Include(o => o.Account)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuMeal)
                        .ThenInclude(mm => mm.Recipe)
                .Include(o => o.DeliverySchedule)
                .Where(o => o.AccountId == accountId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetWithDetailsAsync(Guid orderId)
        {
            return await _dbSet
                .Include(o => o.Account)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.MenuMeal)
                        .ThenInclude(mm => mm.Recipe)
                .Include(o => o.DeliverySchedule)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<decimal> GetTotalRevenueByMonthAsync(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            // Calculate revenue from orders EXCEPT those with non-revenue statuses
            // Exclude: awaiting_online_payment, cancelled, pending, pending_confirmation
            // Include: confirmed, delivering, customer_received, customer_reject, failed, preparing, prepared, etc.
            var excludedStatuses = new[] { "awaiting_online_payment", "cancelled", "pending", "pending_confirmation" };
            
            var orders = await _dbSet
                .Where(o => o.OrderDate >= startDate 
                    && o.OrderDate < endDate 
                    && !excludedStatuses.Contains(o.Status))
                .ToListAsync();

            return orders.Sum(o => o.TotalAmount);
        }
    }
}