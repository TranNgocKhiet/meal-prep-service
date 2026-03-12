using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for HealthProfileAllergy entity operations.
/// Handles CRUD operations for the many-to-many relationship between HealthProfile and Allergy.
/// </summary>
public class HealthProfileAllergyRepository : IHealthProfileAllergyRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<HealthProfileAllergy> _dbSet;

    public HealthProfileAllergyRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<HealthProfileAllergy>();
    }

    /// <summary>
    /// Gets a HealthProfileAllergy by its composite key, including related HealthProfile and Allergy entities.
    /// </summary>
    public async Task<HealthProfileAllergy?> GetByIdAsync(string healthProfileId, string allergyId)
    {
        return await _dbSet
            .Include(hpa => hpa.HealthProfile)
            .Include(hpa => hpa.Allergy)
            .FirstOrDefaultAsync(hpa => hpa.HealthProfileId == healthProfileId && hpa.AllergyId == allergyId);
    }

    /// <summary>
    /// Gets all HealthProfileAllergy records for a specific health profile.
    /// </summary>
    public async Task<List<HealthProfileAllergy>> GetByHealthProfileIdAsync(string healthProfileId)
    {
        return await _dbSet
            .Include(hpa => hpa.HealthProfile)
            .Include(hpa => hpa.Allergy)
            .Where(hpa => hpa.HealthProfileId == healthProfileId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all HealthProfileAllergy records for a specific allergy.
    /// </summary>
    public async Task<List<HealthProfileAllergy>> GetByAllergyIdAsync(string allergyId)
    {
        return await _dbSet
            .Include(hpa => hpa.HealthProfile)
            .Include(hpa => hpa.Allergy)
            .Where(hpa => hpa.AllergyId == allergyId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all HealthProfileAllergy records.
    /// </summary>
    public async Task<List<HealthProfileAllergy>> GetAllAsync()
    {
        return await _dbSet
            .Include(hpa => hpa.HealthProfile)
            .Include(hpa => hpa.Allergy)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new HealthProfileAllergy record.
    /// </summary>
    public async Task<HealthProfileAllergy> AddAsync(HealthProfileAllergy entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Updates an existing HealthProfileAllergy record.
    /// </summary>
    public async Task<HealthProfileAllergy> UpdateAsync(HealthProfileAllergy entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Deletes a HealthProfileAllergy record by its composite key.
    /// </summary>
    public async Task DeleteAsync(string healthProfileId, string allergyId)
    {
        var entity = await _dbSet.FindAsync(healthProfileId, allergyId);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks if a HealthProfileAllergy record exists by its composite key.
    /// </summary>
    public async Task<bool> ExistsAsync(string healthProfileId, string allergyId)
    {
        return await _dbSet.AnyAsync(hpa => hpa.HealthProfileId == healthProfileId && hpa.AllergyId == allergyId);
    }

    /// <summary>
    /// Gets the total count of HealthProfileAllergy records.
    /// </summary>
    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }
}
