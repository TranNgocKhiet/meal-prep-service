using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IFridgeRepository : IRepository<Fridge>
{
    Task<Fridge?> GetByAccountIdAsync(string accountId);
    Task<Fridge?> GetByAccountIdWithItemsAsync(string accountId);
}
