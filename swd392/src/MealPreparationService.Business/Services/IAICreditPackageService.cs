using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IAICreditPackageService
{
    Task<List<AICreditPackageDto>> GetAllAsync();
    Task<AICreditPackageDto> GetByIdAsync(string id);
    Task<AICreditPackageDto> CreateAsync(CreateAICreditPackageDto dto);
    Task<AICreditPackageDto> UpdateAsync(string id, UpdateAICreditPackageDto dto);
    Task DeleteAsync(string id);
}
