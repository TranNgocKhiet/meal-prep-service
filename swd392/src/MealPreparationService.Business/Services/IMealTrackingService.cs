using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IMealTrackingService
{
    Task<List<MealDto>> GetActiveMealsAsync(string userId);
    Task<MealStatusDto> GetMealStatusAsync(string mealId);
    Task<MealFinishCheckDto> CheckMealIngredientsAsync(string mealPlanId, string mealId, string userId);
    Task MarkMealAsFinishedAsync(string mealPlanId, string mealId, string userId);
    Task<MealUnfinishCheckDto> CheckMealUnfinishAsync(string mealPlanId, string mealId, string userId);
    Task MarkMealAsUnfinishedAsync(string mealPlanId, string mealId, string userId, UnfinishMealDto dto);
    Task UpdateExpiredMealsAsync();
    Task<MealPlanProgressDto> GetMealPlanProgressAsync(string mealPlanId);
}
