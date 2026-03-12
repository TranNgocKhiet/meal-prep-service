using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class SubscriptionPackageRepository : Repository<SubscriptionPackage>, ISubscriptionPackageRepository
{
    public SubscriptionPackageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<SubscriptionPackage>> GetActivePackagesAsync()
    {
        return await _dbSet
            .Where(sp => sp.Price > 0 && sp.CreditAmount > 0)
            .OrderBy(sp => sp.Price)
            .ToListAsync();
    }

    public async Task<SubscriptionPackage?> GetByNameAsync(string packageName)
    {
        return await _dbSet
            .FirstOrDefaultAsync(sp => sp.PackageName == packageName);
    }
}
