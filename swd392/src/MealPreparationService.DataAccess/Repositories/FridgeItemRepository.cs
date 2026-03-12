using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for FridgeItem entity operations.
/// </summary>
public class FridgeItemRepository : Repository<FridgeItem>, IFridgeItemRepository
{
    public FridgeItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a fridge item by ID with related entities (Fridge, Account, Ingredient).
    /// </summary>
    public override async Task<FridgeItem?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(fi => fi.Fridge)
            .Include(fi => fi.Account)
            .Include(fi => fi.Ingredient)
            .FirstOrDefaultAsync(fi => fi.Id == id);
    }

    /// <summary>
    /// Gets all fridge items for a specific fridge.
    /// </summary>
    public async Task<List<FridgeItem>> GetByFridgeIdAsync(string fridgeId)
    {
        return await _dbSet
            .Include(fi => fi.Ingredient)
            .Where(fi => fi.FridgeId == fridgeId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all fridge items for a specific account.
    /// </summary>
    public async Task<List<FridgeItem>> GetByAccountIdAsync(string accountId)
    {
        return await _dbSet
            .Include(fi => fi.Ingredient)
            .Include(fi => fi.Fridge)
            .Where(fi => fi.AccountId == accountId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all fridge items expiring before a specified date.
    /// </summary>
    public async Task<List<FridgeItem>> GetExpiringItemsAsync(string fridgeId, DateTime beforeDate)
    {
        return await _dbSet
            .Include(fi => fi.Ingredient)
            .Where(fi => fi.FridgeId == fridgeId && fi.ExpiryDate < beforeDate)
            .OrderBy(fi => fi.ExpiryDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a fridge item by ID with its ingredient.
    /// </summary>
    public async Task<FridgeItem?> GetByIdWithIngredientAsync(string id)
    {
        return await _dbSet
            .Include(fi => fi.Ingredient)
            .FirstOrDefaultAsync(fi => fi.Id == id);
    }
}
