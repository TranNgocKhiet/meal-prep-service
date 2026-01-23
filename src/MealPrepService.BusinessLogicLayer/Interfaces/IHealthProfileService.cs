using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface IHealthProfileService
    {
        Task<HealthProfileDto> CreateOrUpdateAsync(HealthProfileDto dto);
        Task<HealthProfileDto> GetByAccountIdAsync(Guid accountId);
        Task AddAllergyAsync(Guid profileId, Guid allergyId);
        Task RemoveAllergyAsync(Guid profileId, Guid allergyId);
        Task AddFoodPreferenceAsync(Guid profileId, Guid preferenceId);
        Task RemoveFoodPreferenceAsync(Guid profileId, Guid preferenceId);
    }
}