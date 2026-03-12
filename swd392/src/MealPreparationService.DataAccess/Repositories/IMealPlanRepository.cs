using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IMealPlanRepository : IRepository<MealPlan>
{
    Task<List<MealPlan>> GetByUserIdAsync(string userId);
    Task<List<MealPlan>> GetByUserIdAndStatusAsync(string userId, int statusId);
    Task<MealPlan?> GetByIdWithDetailsAsync(string id);
    Task<int> CountByUserIdAsync(string userId);
    Task<Meal?> GetMealByIdAsync(string mealId);
    Task UpdateMealAsync(Meal meal);
}
