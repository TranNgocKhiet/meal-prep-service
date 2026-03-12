using MealPreparationService.Domain.Entities;

namespace MealPreparationService.Business.Services;

public interface ITokenService
{
    string GenerateAccessToken(Account user);
    string GenerateRefreshToken();
    string GenerateRefreshToken(Account user);
    Task<bool> ValidateTokenAsync(string token);
    Task<string?> GetUserIdFromTokenAsync(string token);
}
