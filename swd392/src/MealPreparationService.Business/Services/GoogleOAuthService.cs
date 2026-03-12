using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace MealPreparationService.Business.Services;

public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleOAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string googleToken)
    {
        try
        {
            var clientId = _configuration["GoogleOAuth:ClientId"];
            if (string.IsNullOrEmpty(clientId))
            {
                throw new InvalidOperationException("Google OAuth ClientId is not configured");
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(googleToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });

            return new GoogleUserInfo
            {
                GoogleAuthId = payload.Subject,
                Email = payload.Email,
                FullName = payload.Name,
                Picture = payload.Picture
            };
        }
        catch
        {
            return null;
        }
    }
}
