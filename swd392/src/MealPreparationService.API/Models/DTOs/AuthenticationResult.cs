namespace MealPreparationService.API.Models.DTOs;

public class AuthenticationResult
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}
