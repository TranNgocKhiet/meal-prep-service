using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class AICreditPackageRepository : Repository<AIcreditPackage>, IAICreditPackageRepository
{
    public AICreditPackageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<AIcreditPackage>> GetActivePackagesAsync()
    {
        return await _dbSet
            .Where(acp => acp.Price > 0 && acp.CreditAmount > 0)
            .OrderBy(acp => acp.Price)
            .ToListAsync();
    }

    public async Task<AIcreditPackage?> GetByNameAsync(string packageName)
    {
        return await _dbSet
            .FirstOrDefaultAsync(acp => acp.PackageName == packageName);
    }
}
