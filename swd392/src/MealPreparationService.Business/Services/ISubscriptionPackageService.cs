using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface ISubscriptionPackageService
{
    Task<List<SubscriptionPackageDto>> GetAllAsync();
    Task<SubscriptionPackageDto> GetByIdAsync(string id);
    Task<SubscriptionPackageDto> CreateAsync(CreateSubscriptionPackageDto dto);
    Task<SubscriptionPackageDto> UpdateAsync(string id, UpdateSubscriptionPackageDto dto);
    Task DeleteAsync(string id);
}
