namespace MealPreparationService.Business.Services;

public interface IAllergyCheckService
{
    Task<List<string>> CheckRecipeAllergensAsync(string recipeId, string userId);
    Task<Dictionary<string, List<string>>> CheckMealAllergiesAsync(string userId, List<string> recipeIds);
    Task<bool> HasAllergyWarningAsync(string recipeId, string userId);
}
