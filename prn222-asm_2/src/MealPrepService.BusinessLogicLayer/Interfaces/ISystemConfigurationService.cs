using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces;

public interface ISystemConfigurationService
{
    Task<int> GetMaxMealPlansPerCustomerAsync();
    Task<int> GetMaxFridgeItemsPerCustomerAsync();
    Task<int> GetMaxMealPlanDaysAsync();
    Task UpdateMaxMealPlansAsync(int maxValue, string updatedBy);
    Task UpdateMaxFridgeItemsAsync(int maxValue, string updatedBy);
    Task UpdateMaxMealPlanDaysAsync(int maxDays, string updatedBy);
    Task<Dictionary<string, string>> GetAllConfigurationsAsync();
    Task InitializeDefaultConfigurationsAsync();
}
