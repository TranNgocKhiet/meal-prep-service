using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IAICreditPackageRepository : IRepository<AIcreditPackage>
{
    Task<List<AIcreditPackage>> GetActivePackagesAsync();
    Task<AIcreditPackage?> GetByNameAsync(string packageName);
}
