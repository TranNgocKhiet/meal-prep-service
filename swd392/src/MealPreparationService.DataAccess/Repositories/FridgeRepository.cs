using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class FridgeRepository : Repository<Fridge>, IFridgeRepository
{
    public FridgeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Fridge?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(f => f.Account)
            .Include(f => f.FridgeItems)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Fridge?> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Include(f => f.Account)
            .FirstOrDefaultAsync(f => f.AccountId == accountId);
    }

    public async Task<Fridge?> GetByAccountIdWithItemsAsync(string accountId)
    {
        return await _dbSet
            .Include(f => f.Account)
            .Include(f => f.FridgeItems)
            .FirstOrDefaultAsync(f => f.AccountId == accountId);
    }
}
