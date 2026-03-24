using MealPreparationService.Domain.Services;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MealPreparationService.Business.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeService _dateTimeService;

    public AuthenticationService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IGoogleOAuthService googleOAuthService,
        IConfiguration configuration,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _googleOAuthService = googleOAuthService;
        _configuration = configuration;
        _dateTimeService = dateTimeService;
    }

    public async Task<AuthenticationServiceResult> RegisterAsync(
        string email, 
        string password, 
        string fullName,
        string phoneNumber,
        string roleName)
    {
        // Check if user already exists
        var existingUser = await _unitOfWork.Accounts.GetByEmailAsync(email);
        if (existingUser != null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Email is already registered"
            };
        }

        // Get role by name
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var role = roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        if (role == null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = $"Invalid role: {roleName}"
            };
        }

        // Create new user
        var user = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(password),
            FullName = fullName,
            PhoneNumber = phoneNumber,
            RoleId = role.Id,
            CurrentCredits = 0,
            CreatedAt = _dateTimeService.Now,
            UpdatedAt = _dateTimeService.Now,
            IsActive = true
        };

        await _unitOfWork.Accounts.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Reload user with role
        user = await _unitOfWork.Accounts.GetByIdAsync(user.Id);
        if (user == null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Failed to create user"
            };
        }

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);
        var expiresAt = _dateTimeService.Now.AddHours(int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "24"));

        return new AuthenticationServiceResult
        {
            Success = true,
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthenticationServiceResult> LoginAsync(string email, string password)
    {
        var user = await _unitOfWork.Accounts.GetByEmailAsync(email);
        if (user == null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Wrong email"
            };
        }

        if (!user.IsActive)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Account is inactive"
            };
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Wrong password"
            };
        }

        // Update last login
        user.LastLoginAt = _dateTimeService.Now;
        await _unitOfWork.Accounts.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);
        var expiresAt = _dateTimeService.Now.AddHours(int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "24"));

        return new AuthenticationServiceResult
        {
            Success = true,
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthenticationServiceResult> LoginWithGoogleAsync(string googleToken)
    {
        var googleUserInfo = await _googleOAuthService.ValidateGoogleTokenAsync(googleToken);
        if (googleUserInfo == null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Invalid Google token"
            };
        }

        // Try to find user by Google ID
        var user = await _unitOfWork.Accounts.GetByGoogleIdAsync(googleUserInfo.GoogleAuthId);
        
        if (user == null)
        {
            // Try to find user by email
            user = await _unitOfWork.Accounts.GetByEmailAsync(googleUserInfo.Email);
            
            if (user != null)
            {
                // Check if GoogleAuth record exists
                var existingGoogleAuth = await _unitOfWork.GoogleAuths.GetByIdAsync(googleUserInfo.GoogleAuthId);
                if (existingGoogleAuth == null)
                {
                    // Create GoogleAuth record
                    var googleAuth = new GoogleAuth
                    {
                        Id = googleUserInfo.GoogleAuthId,
                        ProviderKey = googleUserInfo.GoogleAuthId,
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        ExpiresAt = _dateTimeService.Now.AddDays(30),
                        IsVerified = true
                    };
                    await _unitOfWork.GoogleAuths.AddAsync(googleAuth);
                    await _unitOfWork.SaveChangesAsync();
                }
                
                // Link Google account to existing user
                user.GoogleAuthId = googleUserInfo.GoogleAuthId;
                await _unitOfWork.Accounts.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                // Create new user with Customer role
                var roles = await _unitOfWork.Roles.GetAllAsync();
                var customerRole = roles.FirstOrDefault(r => r.Name.Equals("Customer", StringComparison.OrdinalIgnoreCase));
                
                if (customerRole == null)
                {
                    return new AuthenticationServiceResult
                    {
                        Success = false,
                        ErrorMessage = "Customer role not found"
                    };
                }

                // Create GoogleAuth record first
                var googleAuth = new GoogleAuth
                {
                    Id = googleUserInfo.GoogleAuthId,
                    ProviderKey = googleUserInfo.GoogleAuthId,
                    AccessToken = string.Empty,
                    RefreshToken = string.Empty,
                    ExpiresAt = _dateTimeService.Now.AddDays(30),
                    IsVerified = true
                };
                await _unitOfWork.GoogleAuths.AddAsync(googleAuth);
                await _unitOfWork.SaveChangesAsync();

                // Now create the user account
                user = new Account
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = googleUserInfo.Email,
                    PasswordHash = string.Empty, // No password for Google OAuth users
                    FullName = googleUserInfo.FullName,
                    PhoneNumber = string.Empty,
                    RoleId = customerRole.Id,
                    GoogleAuthId = googleUserInfo.GoogleAuthId,
                    CurrentCredits = 0,
                    CreatedAt = _dateTimeService.Now,
                    UpdatedAt = _dateTimeService.Now,
                    IsActive = true
                };

                await _unitOfWork.Accounts.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Reload user with role
                user = await _unitOfWork.Accounts.GetByIdAsync(user.Id);
            }
        }

        if (user == null || !user.IsActive)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "User account is not active"
            };
        }

        // Update last login
        user.LastLoginAt = _dateTimeService.Now;
        await _unitOfWork.Accounts.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);
        var expiresAt = _dateTimeService.Now.AddHours(int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "24"));

        return new AuthenticationServiceResult
        {
            Success = true,
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthenticationServiceResult> RegisterWithGoogleAsync(string googleToken)
    {
        var googleUserInfo = await _googleOAuthService.ValidateGoogleTokenAsync(googleToken);
        if (googleUserInfo == null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Invalid Google token"
            };
        }

        var existingGoogleUser = await _unitOfWork.Accounts.GetByGoogleIdAsync(googleUserInfo.GoogleAuthId);
        if (existingGoogleUser != null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "This Google account is already registered. Please sign in instead."
            };
        }

        var existingEmailUser = await _unitOfWork.Accounts.GetByEmailAsync(googleUserInfo.Email);
        if (existingEmailUser != null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "An account with this email already exists. Please sign in instead."
            };
        }

        var roles = await _unitOfWork.Roles.GetAllAsync();
        var customerRole = roles.FirstOrDefault(r => r.Name.Equals("Customer", StringComparison.OrdinalIgnoreCase));

        if (customerRole == null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Customer role not found"
            };
        }

        var googleAuth = new GoogleAuth
        {
            Id = googleUserInfo.GoogleAuthId,
            ProviderKey = googleUserInfo.GoogleAuthId,
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = _dateTimeService.Now.AddDays(30),
            IsVerified = true
        };
        await _unitOfWork.GoogleAuths.AddAsync(googleAuth);
        await _unitOfWork.SaveChangesAsync();

        var user = new Account
        {
            Id = Guid.NewGuid().ToString(),
            Email = googleUserInfo.Email,
            PasswordHash = string.Empty,
            FullName = googleUserInfo.FullName,
            PhoneNumber = string.Empty,
            RoleId = customerRole.Id,
            GoogleAuthId = googleUserInfo.GoogleAuthId,
            CurrentCredits = 0,
            CreatedAt = _dateTimeService.Now,
            UpdatedAt = _dateTimeService.Now,
            IsActive = true
        };

        await _unitOfWork.Accounts.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        user = await _unitOfWork.Accounts.GetByIdAsync(user.Id);
        if (user == null)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Failed to create user"
            };
        }

        user.LastLoginAt = _dateTimeService.Now;
        await _unitOfWork.Accounts.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user);
        var expiresAt = _dateTimeService.Now.AddHours(int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "24"));

        return new AuthenticationServiceResult
        {
            Success = true,
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        return await _tokenService.ValidateTokenAsync(token);
    }

    public async Task<AuthenticationServiceResult> RefreshTokenAsync(string refreshToken)
    {
        // Extract user ID from the refresh token
        // Since we're using JWT-based refresh tokens without database storage,
        // we validate the token and extract the user ID from it
        var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
        
        if (string.IsNullOrEmpty(userId))
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "Invalid or expired refresh token"
            };
        }

        var user = await _unitOfWork.Accounts.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return new AuthenticationServiceResult
            {
                Success = false,
                ErrorMessage = "User not found or inactive"
            };
        }

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user);
        var expiresAt = _dateTimeService.Now.AddHours(int.Parse(_configuration["JwtSettings:ExpirationHours"] ?? "24"));

        return new AuthenticationServiceResult
        {
            Success = true,
            User = user,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task LogoutAsync(string userId)
    {
        // With JWT-based refresh tokens without database storage,
        // logout is handled client-side by discarding tokens.
        // This method is kept for API compatibility but doesn't need to do anything.
        await Task.CompletedTask;
    }
}


