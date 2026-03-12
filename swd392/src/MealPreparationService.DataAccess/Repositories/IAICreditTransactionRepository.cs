using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IAICreditTransactionRepository : IRepository<AIcreditTransaction>
{
    Task<List<AIcreditTransaction>> GetByAccountIdAsync(string accountId);
    Task<List<AIcreditTransaction>> GetByDateRangeAsync(string accountId, DateTime startDate, DateTime endDate);
}
