using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for HealthProfileIngredient entity operations.
/// Note: HealthProfileIngredient uses a composite key (HealthProfileId, IngredientId) and does not inherit from BaseEntity.
/// </summary>
public interface IHealthProfileIngredientRepository
{
    /// <summary>
    /// Gets a HealthProfileIngredient by its composite key, including related HealthProfile and Ingredient entities.
    /// </summary>
    Task<HealthProfileIngredient?> GetByIdAsync(string healthProfileId, string ingredientId);
    
    /// <summary>
    /// Gets all HealthProfileIngredient records for a specific health profile.
    /// </summary>
    Task<List<HealthProfileIngredient>> GetByHealthProfileIdAsync(string healthProfileId);
    
    /// <summary>
    /// Gets all HealthProfileIngredient records for a specific ingredient.
    /// </summary>
    Task<List<HealthProfileIngredient>> GetByIngredientIdAsync(string ingredientId);
    
    /// <summary>
    /// Gets all HealthProfileIngredient records for a specific health profile filtered by relationship type.
    /// </summary>
    Task<List<HealthProfileIngredient>> GetByRelationshipTypeAsync(string healthProfileId, int relationshipTypeId);
    
    /// <summary>
    /// Gets all HealthProfileIngredient records.
    /// </summary>
    Task<List<HealthProfileIngredient>> GetAllAsync();
    
    /// <summary>
    /// Adds a new HealthProfileIngredient record.
    /// </summary>
    Task<HealthProfileIngredient> AddAsync(HealthProfileIngredient entity);
    
    /// <summary>
    /// Updates an existing HealthProfileIngredient record.
    /// </summary>
    Task<HealthProfileIngredient> UpdateAsync(HealthProfileIngredient entity);
    
    /// <summary>
    /// Deletes a HealthProfileIngredient record by its composite key.
    /// </summary>
    Task DeleteAsync(string healthProfileId, string ingredientId);
    
    /// <summary>
    /// Checks if a HealthProfileIngredient record exists by its composite key.
    /// </summary>
    Task<bool> ExistsAsync(string healthProfileId, string ingredientId);
    
    /// <summary>
    /// Gets the total count of HealthProfileIngredient records.
    /// </summary>
    Task<int> CountAsync();
}
