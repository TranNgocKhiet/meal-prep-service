using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IHealthProfileRepository : IRepository<HealthProfile>
{
    Task<HealthProfile?> GetByAccountIdAsync(string accountId);
    Task<List<HealthProfile>> GetByAccountIdWithAllergiesAsync(string accountId);
}
