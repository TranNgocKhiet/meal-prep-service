using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByAccountIdAsync(string accountId);
    Task<Cart?> GetByAccountIdWithItemsAsync(string accountId);
}
