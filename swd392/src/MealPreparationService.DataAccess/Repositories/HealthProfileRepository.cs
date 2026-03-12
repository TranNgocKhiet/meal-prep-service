using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class HealthProfileRepository : Repository<HealthProfile>, IHealthProfileRepository
{
    public HealthProfileRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<HealthProfile?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(hp => hp.Account)
            .Include(hp => hp.HealthProfileAllergies)
            .Include(hp => hp.HealthProfileIngredients)
            .FirstOrDefaultAsync(hp => hp.Id == id);
    }

    public async Task<HealthProfile?> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Include(hp => hp.Account)
            .Include(hp => hp.HealthProfileAllergies)
            .Include(hp => hp.HealthProfileIngredients)
            .FirstOrDefaultAsync(hp => hp.AccountId == accountId);
    }

    public async Task<List<HealthProfile>> GetByAccountIdWithAllergiesAsync(string accountId)
    {
        return await _dbSet
            .Include(hp => hp.Account)
            .Include(hp => hp.HealthProfileAllergies)
                .ThenInclude(hpa => hpa.Allergy)
            .Where(hp => hp.AccountId == accountId)
            .ToListAsync();
    }
}
