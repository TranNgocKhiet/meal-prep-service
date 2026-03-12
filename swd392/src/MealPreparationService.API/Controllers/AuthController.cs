using MealPreparationService.API.Models;
using MealPreparationService.API.Models.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthenticationResult>>> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authenticationService.RegisterAsync(
                dto.Email,
                dto.Password,
                dto.FullName,
                dto.PhoneNumber,
                dto.RoleName);

            if (!result.Success)
            {
                return BadRequest(ApiResponse<AuthenticationResult>.ErrorResponse(result.ErrorMessage ?? "Registration failed"));
            }

            var authResult = new AuthenticationResult
            {
                Success = true,
                Token = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresAt = result.ExpiresAt!.Value,
                User = new UserDto
                {
                    Id = result.User!.Id,
                    Email = result.User.Email,
                    FullName = result.User.FullName,
                    PhoneNumber = result.User.PhoneNumber,
                    RoleName = result.User.Role.Name
                }
            };

            return Ok(ApiResponse<AuthenticationResult>.SuccessResponse(authResult, "Registration successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, ApiResponse<AuthenticationResult>.ErrorResponse("An error occurred during registration"));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthenticationResult>>> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authenticationService.LoginAsync(dto.Email, dto.Password);

            if (!result.Success)
            {
                return Unauthorized(ApiResponse<AuthenticationResult>.ErrorResponse(result.ErrorMessage ?? "Login failed"));
            }

            var authResult = new AuthenticationResult
            {
                Success = true,
                Token = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresAt = result.ExpiresAt!.Value,
                User = new UserDto
                {
                    Id = result.User!.Id,
                    Email = result.User.Email,
                    FullName = result.User.FullName,
                    PhoneNumber = result.User.PhoneNumber,
                    RoleName = result.User.Role.Name
                }
            };

            return Ok(ApiResponse<AuthenticationResult>.SuccessResponse(authResult, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, ApiResponse<AuthenticationResult>.ErrorResponse("An error occurred during login"));
        }
    }

    [HttpPost("google-login")]
    public async Task<ActionResult<ApiResponse<AuthenticationResult>>> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var result = await _authenticationService.LoginWithGoogleAsync(dto.GoogleToken);

            if (!result.Success)
            {
                return Unauthorized(ApiResponse<AuthenticationResult>.ErrorResponse(result.ErrorMessage ?? "Google login failed"));
            }

            var authResult = new AuthenticationResult
            {
                Success = true,
                Token = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresAt = result.ExpiresAt!.Value,
                User = new UserDto
                {
                    Id = result.User!.Id,
                    Email = result.User.Email,
                    FullName = result.User.FullName,
                    PhoneNumber = result.User.PhoneNumber,
                    RoleName = result.User.Role.Name
                }
            };

            return Ok(ApiResponse<AuthenticationResult>.SuccessResponse(authResult, "Google login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return StatusCode(500, ApiResponse<AuthenticationResult>.ErrorResponse("An error occurred during Google login"));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<TokenDto>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authenticationService.RefreshTokenAsync(request.RefreshToken);

            if (!result.Success)
            {
                return Unauthorized(ApiResponse<TokenDto>.ErrorResponse(result.ErrorMessage ?? "Token refresh failed"));
            }

            var tokenDto = new TokenDto
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresAt = result.ExpiresAt!.Value
            };

            return Ok(ApiResponse<TokenDto>.SuccessResponse(tokenDto, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiResponse<TokenDto>.ErrorResponse("An error occurred during token refresh"));
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _authenticationService.LogoutAsync(userId);

            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Logout successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred during logout"));
        }
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var fullName = User.FindFirst(ClaimTypes.Name)?.Value;
            var roleName = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<UserDto>.ErrorResponse("User not authenticated"));
            }

            var userDto = new UserDto
            {
                Id = userId,
                Email = email ?? string.Empty,
                FullName = fullName ?? string.Empty,
                RoleName = roleName ?? string.Empty
            };

            return Ok(ApiResponse<UserDto>.SuccessResponse(userDto, "User retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("An error occurred while retrieving user"));
        }
    }

    /// <summary>
    /// Deletes the current user's account and anonymizes their data
    /// Validates: Requirements 22.7
    /// </summary>
    [Authorize]
    [HttpDelete("delete-account")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAccount([FromServices] IUserDataService userDataService)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await userDataService.DeleteUserAccountAsync(userId);

            _logger.LogInformation("Account deleted and data anonymized for user {UserId}", userId);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { }, 
                "Your account has been deleted and your personal data has been anonymized. Transaction records have been retained for compliance purposes."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during account deletion");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting your account"));
        }
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
