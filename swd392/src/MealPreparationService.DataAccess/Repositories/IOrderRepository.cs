using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IOrderRepository : IRepository<Order>
{
    Task<List<Order>> GetByUserIdAsync(string userId);
    Task<List<Order>> GetByStatusAsync(int statusId);
    Task<Order?> GetByIdWithDetailsAsync(string id);
}
