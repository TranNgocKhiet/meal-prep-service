using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<Cart?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(c => c.Account)
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cart?> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.AccountId == accountId);
    }

    public async Task<Cart?> GetByAccountIdWithItemsAsync(string accountId)
    {
        return await _dbSet
            .Include(c => c.Account)
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.AccountId == accountId);
    }
}
