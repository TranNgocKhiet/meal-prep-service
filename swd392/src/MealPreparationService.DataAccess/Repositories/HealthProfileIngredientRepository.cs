using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for HealthProfileIngredient entity operations.
/// Handles CRUD operations for the many-to-many relationship between HealthProfile and Ingredient.
/// </summary>
public class HealthProfileIngredientRepository : IHealthProfileIngredientRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<HealthProfileIngredient> _dbSet;

    public HealthProfileIngredientRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<HealthProfileIngredient>();
    }

    /// <summary>
    /// Gets a HealthProfileIngredient by its composite key, including related HealthProfile and Ingredient entities.
    /// </summary>
    public async Task<HealthProfileIngredient?> GetByIdAsync(string healthProfileId, string ingredientId)
    {
        return await _dbSet
            .Include(hpi => hpi.HealthProfile)
            .Include(hpi => hpi.Ingredient)
            .FirstOrDefaultAsync(hpi => hpi.HealthProfileId == healthProfileId && hpi.IngredientId == ingredientId);
    }

    /// <summary>
    /// Gets all HealthProfileIngredient records for a specific health profile.
    /// </summary>
    public async Task<List<HealthProfileIngredient>> GetByHealthProfileIdAsync(string healthProfileId)
    {
        return await _dbSet
            .Include(hpi => hpi.HealthProfile)
            .Include(hpi => hpi.Ingredient)
            .Where(hpi => hpi.HealthProfileId == healthProfileId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all HealthProfileIngredient records for a specific ingredient.
    /// </summary>
    public async Task<List<HealthProfileIngredient>> GetByIngredientIdAsync(string ingredientId)
    {
        return await _dbSet
            .Include(hpi => hpi.HealthProfile)
            .Include(hpi => hpi.Ingredient)
            .Where(hpi => hpi.IngredientId == ingredientId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all HealthProfileIngredient records for a specific health profile filtered by relationship type.
    /// </summary>
    public async Task<List<HealthProfileIngredient>> GetByRelationshipTypeAsync(string healthProfileId, int relationshipTypeId)
    {
        return await _dbSet
            .Include(hpi => hpi.HealthProfile)
            .Include(hpi => hpi.Ingredient)
            .Include(hpi => hpi.RelationshipType)
            .Where(hpi => hpi.HealthProfileId == healthProfileId && hpi.RelationshipTypeId == relationshipTypeId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all HealthProfileIngredient records.
    /// </summary>
    public async Task<List<HealthProfileIngredient>> GetAllAsync()
    {
        return await _dbSet
            .Include(hpi => hpi.HealthProfile)
            .Include(hpi => hpi.Ingredient)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new HealthProfileIngredient record.
    /// </summary>
    public async Task<HealthProfileIngredient> AddAsync(HealthProfileIngredient entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Updates an existing HealthProfileIngredient record.
    /// </summary>
    public async Task<HealthProfileIngredient> UpdateAsync(HealthProfileIngredient entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Deletes a HealthProfileIngredient record by its composite key.
    /// </summary>
    public async Task DeleteAsync(string healthProfileId, string ingredientId)
    {
        var entity = await _dbSet.FindAsync(healthProfileId, ingredientId);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks if a HealthProfileIngredient record exists by its composite key.
    /// </summary>
    public async Task<bool> ExistsAsync(string healthProfileId, string ingredientId)
    {
        return await _dbSet.AnyAsync(hpi => hpi.HealthProfileId == healthProfileId && hpi.IngredientId == ingredientId);
    }

    /// <summary>
    /// Gets the total count of HealthProfileIngredient records.
    /// </summary>
    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
}
