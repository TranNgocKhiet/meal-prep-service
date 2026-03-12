using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class AICreditTransactionRepository : Repository<AIcreditTransaction>, IAICreditTransactionRepository
{
    public AICreditTransactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<AIcreditTransaction?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(act => act.Account)
            .Include(act => act.AIcreditPackage)
            .Include(act => act.PaymentGateway)
            .FirstOrDefaultAsync(act => act.Id == id);
    }

    public async Task<List<AIcreditTransaction>> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Include(act => act.AIcreditPackage)
            .Include(act => act.PaymentGateway)
            .Where(act => act.AccountId == accountId)
            .OrderByDescending(act => act.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AIcreditTransaction>> GetByDateRangeAsync(string accountId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(act => act.AIcreditPackage)
            .Include(act => act.PaymentGateway)
            .Where(act => act.AccountId == accountId && act.CreatedAt >= startDate && act.CreatedAt <= endDate)
            .OrderByDescending(act => act.CreatedAt)
            .ToListAsync();
    }
}
