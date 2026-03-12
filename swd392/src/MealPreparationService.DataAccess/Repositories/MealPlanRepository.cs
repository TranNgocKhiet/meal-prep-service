using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class MealPlanRepository : Repository<MealPlan>, IMealPlanRepository
{
    public MealPlanRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<MealPlan>> GetByUserIdAsync(string userId)
    {
        return await _dbSet
            .Where(mp => mp.AccountId == userId)
            .OrderByDescending(mp => mp.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MealPlan>> GetByUserIdAndStatusAsync(string userId, int statusId)
    {
        return await _dbSet
            .Where(mp => mp.AccountId == userId && mp.IsActive == (statusId == 1))
            .OrderByDescending(mp => mp.CreatedAt)
            .ToListAsync();
    }

    public async Task<MealPlan?> GetByIdWithDetailsAsync(string id)
    {
        return await _dbSet
            .Include(mp => mp.Account)
            .Include(mp => mp.Meals)
            .FirstOrDefaultAsync(mp => mp.Id == id);
    }

    public async Task<int> CountByUserIdAsync(string userId)
    {
        return await _dbSet.CountAsync(mp => mp.AccountId == userId);
    }

    public override async Task<MealPlan?> GetByIdAsync(string id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(mp => mp.Id == id);
    }

    public async Task<Meal?> GetMealByIdAsync(string mealId)
    {
        return await _context.Set<Meal>()
            .Include(m => m.MealRecipes)
                .ThenInclude(mr => mr.Recipe)
            .FirstOrDefaultAsync(m => m.Id == mealId);
    }

    public Task UpdateMealAsync(Meal meal)
    {
        _context.Set<Meal>().Update(meal);
        return Task.CompletedTask;
    }
}
