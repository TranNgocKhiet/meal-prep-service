using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Tests;

/// <summary>
/// Property-based tests for AccountService
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class AccountServicePropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IPasswordHasher _passwordHasher;
    private IAccountService _accountService;
    private Mock<ILogger<AccountService>> _mockLogger;

    public AccountServicePropertyTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _passwordHasher = new PasswordHasher();
        _mockLogger = new Mock<ILogger<AccountService>>();
        _accountService = new AccountService(_unitOfWork, _passwordHasher, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _unitOfWork.Dispose();
    }

    /// <summary>
    /// Property 1: Account creation sets customer role
    /// For any valid account registration, the created account should have role "Customer"
    /// Validates: Requirements 1.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AccountCreationSetsCustomerRole()
    {
        return Prop.ForAll(
            GenerateValidCreateAccountDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var passwordHasher = new PasswordHasher();
                var mockLogger = new Mock<ILogger<AccountService>>();
                var accountService = new AccountService(unitOfWork, passwordHasher, mockLogger.Object);

                // Act: Register account
                var result = accountService.RegisterAsync(dto).Result;

                // Assert: Account should have Customer role
                return result != null
                    && result.Role == "Customer"
                    && result.Email == dto.Email
                    && result.FullName == dto.FullName;
            });
    }

    /// <summary>
    /// Property 2: Duplicate email rejection
    /// For any email that already exists, attempting to register with that email should throw BusinessException
    /// Validates: Requirements 1.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DuplicateEmailRejection()
    {
        return Prop.ForAll(
            GenerateValidCreateAccountDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var passwordHasher = new PasswordHasher();
                var mockLogger = new Mock<ILogger<AccountService>>();
                var accountService = new AccountService(unitOfWork, passwordHasher, mockLogger.Object);

                // Act: Register account first time
                accountService.RegisterAsync(dto).Wait();

                // Try to register with same email
                var duplicateDto = new CreateAccountDto
                {
                    Email = dto.Email,
                    Password = "DifferentPassword123!",
                    FullName = "Different Name"
                };

                // Assert: Should throw BusinessException
                try
                {
                    accountService.RegisterAsync(duplicateDto).Wait();
                    return false; // Should not reach here
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    return ex.Message.Contains("Email already exists");
                }
                catch
                {
                    return false; // Wrong exception type
                }
            });
    }

    /// <summary>
    /// Property 3: Authentication with valid credentials
    /// For any registered account, authenticating with correct credentials should succeed
    /// Validates: Requirements 1.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AuthenticationWithValidCredentials()
    {
        return Prop.ForAll(
            GenerateValidCreateAccountDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var passwordHasher = new PasswordHasher();
                var mockLogger = new Mock<ILogger<AccountService>>();
                var accountService = new AccountService(unitOfWork, passwordHasher, mockLogger.Object);

                // Act: Register account
                var registeredAccount = accountService.RegisterAsync(dto).Result;

                // Authenticate with correct credentials
                var authenticatedAccount = accountService.AuthenticateAsync(dto.Email, dto.Password).Result;

                // Assert: Authentication should succeed and return same account
                return authenticatedAccount != null
                    && authenticatedAccount.Id == registeredAccount.Id
                    && authenticatedAccount.Email == dto.Email
                    && authenticatedAccount.FullName == dto.FullName
                    && authenticatedAccount.Role == "Customer";
            });
    }

    /// <summary>
    /// Property 4: Authentication with invalid credentials
    /// For any registered account, authenticating with incorrect password should throw AuthenticationException
    /// Validates: Requirements 1.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AuthenticationWithInvalidCredentials()
    {
        return Prop.ForAll(
            GenerateValidCreateAccountDto(),
            Arb.From(GenerateNonEmptyString(8, 20)),
            (dto, wrongPassword) =>
            {
                // Ensure wrong password is different from correct password
                if (wrongPassword == dto.Password)
                {
                    wrongPassword = dto.Password + "X";
                }

                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var passwordHasher = new PasswordHasher();
                var mockLogger = new Mock<ILogger<AccountService>>();
                var accountService = new AccountService(unitOfWork, passwordHasher, mockLogger.Object);

                // Act: Register account
                accountService.RegisterAsync(dto).Wait();

                // Try to authenticate with wrong password
                try
                {
                    accountService.AuthenticateAsync(dto.Email, wrongPassword).Wait();
                    return false; // Should not reach here
                }
                catch (AggregateException ae) when (ae.InnerException is AuthenticationException ex)
                {
                    return ex.Message.Contains("Invalid credentials");
                }
                catch
                {
                    return false; // Wrong exception type
                }
            });
    }

    /// <summary>
    /// Property 5: Password hashing
    /// For any password, the stored password hash should be different from the original password
    /// and should be verifiable
    /// Validates: Requirements 1.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PasswordHashing()
    {
        return Prop.ForAll(
            GenerateValidCreateAccountDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var passwordHasher = new PasswordHasher();
                var mockLogger = new Mock<ILogger<AccountService>>();
                var accountService = new AccountService(unitOfWork, passwordHasher, mockLogger.Object);

                // Act: Register account
                var registeredAccount = accountService.RegisterAsync(dto).Result;

                // Retrieve the account from database to check password hash
                var accountEntity = unitOfWork.Accounts.GetByIdAsync(registeredAccount.Id).Result;

                // Assert: Password hash should be different from original password
                // and should be verifiable
                return accountEntity != null
                    && accountEntity.PasswordHash != dto.Password
                    && !string.IsNullOrEmpty(accountEntity.PasswordHash)
                    && passwordHasher.VerifyPassword(dto.Password, accountEntity.PasswordHash)
                    && !passwordHasher.VerifyPassword(dto.Password + "X", accountEntity.PasswordHash);
            });
    }

    #region Generators

    /// <summary>
    /// Generator for valid CreateAccountDto
    /// </summary>
    private static Arbitrary<CreateAccountDto> GenerateValidCreateAccountDto()
    {
        var gen = from email in GenerateValidEmail()
                  from password in GenerateValidPassword()
                  from fullName in GenerateNonEmptyString(3, 50)
                  select new CreateAccountDto
                  {
                      Email = email,
                      Password = password,
                      FullName = fullName
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate a valid email address
    /// </summary>
    private static Gen<string> GenerateValidEmail()
    {
        return from username in GenerateNonEmptyString(3, 20)
               from domain in GenerateNonEmptyString(3, 20)
               select $"{username.Replace(" ", "")}@{domain.Replace(" ", "")}.com";
    }

    /// <summary>
    /// Generate a valid password (at least 8 characters)
    /// </summary>
    private static Gen<string> GenerateValidPassword()
    {
        return from password in GenerateNonEmptyString(8, 30)
               select password;
    }

    /// <summary>
    /// Generate a non-empty string with specified length range
    /// </summary>
    private static Gen<string> GenerateNonEmptyString(int minLength, int maxLength)
    {
        return from length in Gen.Choose(minLength, maxLength)
               from chars in Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray()))
               let str = new string(chars).Trim()
               where !string.IsNullOrWhiteSpace(str) && str.Length >= minLength
               select str.Length > maxLength ? str.Substring(0, maxLength) : str;
    }

    #endregion
}
