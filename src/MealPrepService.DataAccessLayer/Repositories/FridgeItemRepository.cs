using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository implementation for FridgeItem entity
    /// </summary>
    public class FridgeItemRepository : Repository<FridgeItem>, IFridgeItemRepository
    {
        public FridgeItemRepository(MealPrepDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<FridgeItem>> GetByAccountIdAsync(Guid accountId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(fi => fi.Ingredient)
                .Where(fi => fi.AccountId == accountId)
                .OrderBy(fi => fi.ExpiryDate)
                .ToListAsync();
        }

        public async Task<(IEnumerable<FridgeItem> Items, int TotalCount)> GetByAccountIdPagedAsync(Guid accountId, int pageNumber, int pageSize)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(fi => fi.Ingredient)
                .Where(fi => fi.AccountId == accountId)
                .OrderBy(fi => fi.ExpiryDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<FridgeItem>> GetExpiringItemsAsync(Guid accountId, int daysThreshold)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
            return await _dbSet
                .AsNoTracking()
                .Include(fi => fi.Ingredient)
                .Where(fi => fi.AccountId == accountId 
                    && fi.ExpiryDate <= thresholdDate)
                .OrderBy(fi => fi.ExpiryDate)
                .ToListAsync();
        }
    }
}