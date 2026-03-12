using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for HealthProfileAllergy entity operations.
/// Note: HealthProfileAllergy uses a composite key (HealthProfileId, AllergyId) and does not inherit from BaseEntity.
/// </summary>
public interface IHealthProfileAllergyRepository
{
    /// <summary>
    /// Gets a HealthProfileAllergy by its composite key, including related HealthProfile and Allergy entities.
    /// </summary>
    Task<HealthProfileAllergy?> GetByIdAsync(string healthProfileId, string allergyId);
    
    /// <summary>
    /// Gets all HealthProfileAllergy records for a specific health profile.
    /// </summary>
    Task<List<HealthProfileAllergy>> GetByHealthProfileIdAsync(string healthProfileId);
    
    /// <summary>
    /// Gets all HealthProfileAllergy records for a specific allergy.
    /// </summary>
    Task<List<HealthProfileAllergy>> GetByAllergyIdAsync(string allergyId);
    
    /// <summary>
    /// Gets all HealthProfileAllergy records.
    /// </summary>
    Task<List<HealthProfileAllergy>> GetAllAsync();
    
    /// <summary>
    /// Adds a new HealthProfileAllergy record.
    /// </summary>
    Task<HealthProfileAllergy> AddAsync(HealthProfileAllergy entity);
    
    /// <summary>
    /// Updates an existing HealthProfileAllergy record.
    /// </summary>
    Task<HealthProfileAllergy> UpdateAsync(HealthProfileAllergy entity);
    
    /// <summary>
    /// Deletes a HealthProfileAllergy record by its composite key.
    /// </summary>
    Task DeleteAsync(string healthProfileId, string allergyId);
    
    /// <summary>
    /// Checks if a HealthProfileAllergy record exists by its composite key.
    /// </summary>
    Task<bool> ExistsAsync(string healthProfileId, string allergyId);
    
    /// <summary>
    /// Gets the total count of HealthProfileAllergy records.
    /// </summary>
    Task<int> CountAsync();
}
