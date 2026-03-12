using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for nutrient calculation operations
/// </summary>
public interface INutrientCalculatorService
{
    /// <summary>
    /// Calculates nutritional information for ingredients using AI
    /// </summary>
    Task<NutrientCalculationDto> CalculateNutrientsAsync(NutrientRequestDto dto, string userId);
    
    /// <summary>
    /// Gets a saved nutrient calculation by ID
    /// </summary>
    Task<NutrientCalculationDto?> GetSavedCalculationAsync(string calculationId, string userId);
    
    /// <summary>
    /// Gets user's calculation history
    /// </summary>
    Task<List<NutrientCalculationDto>> GetUserCalculationsAsync(string userId);
    
    /// <summary>
    /// Saves a nutrient calculation for future reference
    /// </summary>
    Task SaveCalculationAsync(string userId, string calculationId);
}
