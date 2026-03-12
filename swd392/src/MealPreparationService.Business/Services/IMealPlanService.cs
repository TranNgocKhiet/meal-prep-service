using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IMealPlanService
{
    Task<MealPlanDto> CreateCustomMealPlanAsync(CreateMealPlanDto dto, string userId);
    Task<MealPlanDto> CreateAiGeneratedMealPlanAsync(AiMealPlanRequestDto dto, string userId);
    Task<MealPlanDto> UpdateMealPlanAsync(string mealPlanId, UpdateMealPlanDto dto, string userId);
    Task DeleteMealPlanAsync(string mealPlanId, string userId);
    Task<MealPlanDto?> GetMealPlanByIdAsync(string mealPlanId, string userId);
    Task<List<MealPlanDto>> GetUserMealPlansAsync(string userId);
    Task<bool> ValidateMealPlanLimitsAsync(string userId);
    Task<MealPlanDto> SetActiveMealPlanAsync(string mealPlanId, string userId);
}
