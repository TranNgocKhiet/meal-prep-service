using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IUserSubscriptionRepository : IRepository<UserSubscription>
{
    Task<List<UserSubscription>> GetByAccountIdAsync(string accountId);
    Task<UserSubscription?> GetActiveSubscriptionAsync(string accountId);
    Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate);
}
