using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface ISubscriptionPackageRepository : IRepository<SubscriptionPackage>
{
    Task<List<SubscriptionPackage>> GetActivePackagesAsync();
    Task<SubscriptionPackage?> GetByNameAsync(string packageName);
}
