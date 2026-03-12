using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IStatusRepository
{
    Task<Status?> GetByIdAsync(int id);
    Task<Status?> GetByNameAsync(string name);
    Task<List<Status>> GetAllAsync();
    Task<Status> AddAsync(Status entity);
    Task<Status> UpdateAsync(Status entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> CountAsync();
}
