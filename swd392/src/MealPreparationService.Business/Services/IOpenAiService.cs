using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for OpenAI API integration
/// </summary>
public interface IOpenAiService
{
    /// <summary>
    /// Generates AI meal plan suggestions based on user context
    /// </summary>
    Task<AiMealPlanResponseDto> GenerateMealPlanAsync(AiMealPlanPromptDto prompt);
    
    /// <summary>
    /// Calculates nutritional information for ingredients
    /// </summary>
    Task<NutrientDataDto> CalculateNutrientsAsync(NutrientPromptDto prompt);
    
    /// <summary>
    /// Gets health advice based on context
    /// </summary>
    Task<string> GetHealthAdviceAsync(string context);
}
