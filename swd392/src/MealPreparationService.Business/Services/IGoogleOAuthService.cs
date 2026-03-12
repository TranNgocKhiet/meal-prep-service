namespace MealPreparationService.Business.Services;

public interface IGoogleOAuthService
{
    Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string googleToken);
}

public class GoogleUserInfo
{
    public string GoogleAuthId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Picture { get; set; }
}
