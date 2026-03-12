using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for MenuMealRecipe entity operations.
/// Handles CRUD operations for the many-to-many relationship between MenuMeal and Recipe.
/// </summary>
public class MenuMealRecipeRepository : IMenuMealRecipeRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<MenuMealRecipe> _dbSet;

    public MenuMealRecipeRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<MenuMealRecipe>();
    }

    /// <summary>
    /// Gets a MenuMealRecipe by its composite key, including related MenuMeal and Recipe entities.
    /// </summary>
    public async Task<MenuMealRecipe?> GetByIdAsync(string menuMealId, string recipeId)
    {
        return await _dbSet
            .Include(mmr => mmr.MenuMeal)
            .Include(mmr => mmr.Recipe)
            .FirstOrDefaultAsync(mmr => mmr.MenuMealId == menuMealId && mmr.RecipeId == recipeId);
    }

    /// <summary>
    /// Gets all MenuMealRecipe records for a specific menu meal.
    /// </summary>
    public async Task<List<MenuMealRecipe>> GetByMenuMealIdAsync(string menuMealId)
    {
        return await _dbSet
            .Include(mmr => mmr.MenuMeal)
            .Include(mmr => mmr.Recipe)
            .Where(mmr => mmr.MenuMealId == menuMealId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all MenuMealRecipe records for a specific recipe.
    /// </summary>
    public async Task<List<MenuMealRecipe>> GetByRecipeIdAsync(string recipeId)
    {
        return await _dbSet
            .Include(mmr => mmr.MenuMeal)
            .Include(mmr => mmr.Recipe)
            .Where(mmr => mmr.RecipeId == recipeId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all MenuMealRecipe records.
    /// </summary>
    public async Task<List<MenuMealRecipe>> GetAllAsync()
    {
        return await _dbSet
            .Include(mmr => mmr.MenuMeal)
            .Include(mmr => mmr.Recipe)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new MenuMealRecipe record.
    /// </summary>
    public async Task<MenuMealRecipe> AddAsync(MenuMealRecipe entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Updates an existing MenuMealRecipe record.
    /// </summary>
    public async Task<MenuMealRecipe> UpdateAsync(MenuMealRecipe entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Deletes a MenuMealRecipe record by its composite key.
    /// </summary>
    public async Task DeleteAsync(string menuMealId, string recipeId)
    {
        var entity = await _dbSet.FindAsync(menuMealId, recipeId);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks if a MenuMealRecipe record exists by its composite key.
    /// </summary>
    public async Task<bool> ExistsAsync(string menuMealId, string recipeId)
    {
        return await _dbSet.AnyAsync(mmr => mmr.MenuMealId == menuMealId && mmr.RecipeId == recipeId);
    }

    /// <summary>
    /// Gets the total count of MenuMealRecipe records.
    /// </summary>
    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
}
