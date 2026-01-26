using MealPrepService.DataAccessLayer.Entities;

namespace MealPrepService.DataAccessLayer.Repositories
{
    /// <summary>
    /// Specialized repository interface for FridgeItem entity
    /// </summary>
    public interface IFridgeItemRepository : IRepository<FridgeItem>
    {
        Task<IEnumerable<FridgeItem>> GetByAccountIdAsync(Guid accountId);
        Task<(IEnumerable<FridgeItem> Items, int TotalCount)> GetByAccountIdPagedAsync(Guid accountId, int pageNumber, int pageSize);
        Task<IEnumerable<FridgeItem>> GetExpiringItemsAsync(Guid accountId, int daysThreshold);
    }
}