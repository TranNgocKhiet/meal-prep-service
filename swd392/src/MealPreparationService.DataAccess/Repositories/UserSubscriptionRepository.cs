using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class UserSubscriptionRepository : Repository<UserSubscription>, IUserSubscriptionRepository
{
    public UserSubscriptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<UserSubscription?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(us => us.Account)
            .Include(us => us.SubscriptionPackage)
            .Include(us => us.PaymentGateway)
            .FirstOrDefaultAsync(us => us.Id == id);
    }

    public async Task<List<UserSubscription>> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Include(us => us.SubscriptionPackage)
            .Include(us => us.PaymentGateway)
            .Where(us => us.AccountId == accountId)
            .OrderByDescending(us => us.StartDate)
            .ToListAsync();
    }

    public async Task<UserSubscription?> GetActiveSubscriptionAsync(string accountId)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        return await _dbSet
            .Include(us => us.SubscriptionPackage)
            .Include(us => us.PaymentGateway)
            .Where(us => us.AccountId == accountId && us.StartDate <= now && us.EndDate >= now)
            .OrderByDescending(us => us.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(DateTime beforeDate)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        return await _dbSet
            .Include(us => us.Account)
            .Include(us => us.SubscriptionPackage)
            .Where(us => us.EndDate >= now && us.EndDate <= beforeDate)
            .OrderBy(us => us.EndDate)
            .ToListAsync();
    }
}

