using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IAllergyService
{
    Task AddUserAllergyAsync(string userId, string allergyId);
    Task RemoveUserAllergyAsync(string userId, string allergyId);
    Task<List<AllergyDto>> GetUserAllergiesAsync(string userId);
    Task<List<string>> CheckRecipeAllergensAsync(string recipeId, string userId);
    Task<bool> HasAllergyWarningAsync(string recipeId, string userId);
    Task<List<AllergyDto>> GetAllAllergiesAsync();
}
