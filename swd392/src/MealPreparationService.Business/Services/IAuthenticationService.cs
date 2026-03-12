using MealPreparationService.Domain.Entities;

namespace MealPreparationService.Business.Services;

public interface IAuthenticationService
{
    Task<AuthenticationServiceResult> RegisterAsync(string email, string password, string fullName, string phoneNumber, string roleName);
    Task<AuthenticationServiceResult> LoginAsync(string email, string password);
    Task<AuthenticationServiceResult> LoginWithGoogleAsync(string googleToken);
    Task<bool> ValidateTokenAsync(string token);
    Task<AuthenticationServiceResult> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string userId);
}

public class AuthenticationServiceResult
{
    public bool Success { get; set; }
    public Account? User { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}
