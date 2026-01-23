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
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Tests;

/// <summary>
/// Property-based tests for HealthProfileService
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class HealthProfileServicePropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IHealthProfileService _healthProfileService;
    private Mock<ILogger<HealthProfileService>> _mockLogger;

    public HealthProfileServicePropertyTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockLogger = new Mock<ILogger<HealthProfileService>>();
        _healthProfileService = new HealthProfileService(_unitOfWork, _mockLogger.Object, _context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _unitOfWork.Dispose();
    }

    /// <summary>
    /// Property 7: Health profile storage
    /// For any valid health profile data, the system should store and retrieve it correctly
    /// Validates: Requirements 2.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property HealthProfileStorage()
    {
        return Prop.ForAll(
            GenerateValidHealthProfileDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<HealthProfileService>>();
                var healthProfileService = new HealthProfileService(unitOfWork, mockLogger.Object, context);

                // Create account first
                var account = new Account
                {
                    Id = dto.AccountId,
                    Email = $"test{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Test User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Accounts.AddAsync(account).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Act: Create health profile
                var createdProfile = healthProfileService.CreateOrUpdateAsync(dto).Result;

                // Retrieve the profile
                var retrievedProfile = healthProfileService.GetByAccountIdAsync(dto.AccountId).Result;

                // Assert: Profile should be stored and retrieved correctly
                return createdProfile != null
                    && retrievedProfile != null
                    && retrievedProfile.AccountId == dto.AccountId
                    && retrievedProfile.Age == dto.Age
                    && retrievedProfile.Weight == dto.Weight
                    && retrievedProfile.Height == dto.Height
                    && retrievedProfile.Gender == dto.Gender
                    && retrievedProfile.HealthNotes == dto.HealthNotes;
            });
    }

    /// <summary>
    /// Property 8: Allergy and preference link management
    /// For any health profile, adding and removing allergies and preferences should work correctly
    /// Validates: Requirements 2.2, 2.3, 2.4, 2.5, 2.6, 2.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllergyAndPreferenceLinkManagement()
    {
        return Prop.ForAll(
            GenerateValidHealthProfileDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<HealthProfileService>>();
                var healthProfileService = new HealthProfileService(unitOfWork, mockLogger.Object, context);

                // Create account first
                var account = new Account
                {
                    Id = dto.AccountId,
                    Email = $"test{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Test User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Accounts.AddAsync(account).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Create health profile
                var createdProfile = healthProfileService.CreateOrUpdateAsync(dto).Result;

                // Create allergy and food preference
                var allergy = new Allergy
                {
                    Id = Guid.NewGuid(),
                    AllergyName = "Peanuts",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Allergies.AddAsync(allergy).Wait();

                var foodPreference = new FoodPreference
                {
                    Id = Guid.NewGuid(),
                    PreferenceName = "Vegetarian",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.FoodPreferences.AddAsync(foodPreference).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Act: Add allergy and food preference
                healthProfileService.AddAllergyAsync(createdProfile.Id, allergy.Id).Wait();
                healthProfileService.AddFoodPreferenceAsync(createdProfile.Id, foodPreference.Id).Wait();

                // Retrieve profile with links
                var profileWithLinks = healthProfileService.GetByAccountIdAsync(dto.AccountId).Result;

                // Verify links were added
                var hasAllergy = profileWithLinks.Allergies.Contains("Peanuts");
                var hasPreference = profileWithLinks.FoodPreferences.Contains("Vegetarian");

                // Remove allergy and food preference
                healthProfileService.RemoveAllergyAsync(createdProfile.Id, allergy.Id).Wait();
                healthProfileService.RemoveFoodPreferenceAsync(createdProfile.Id, foodPreference.Id).Wait();

                // Retrieve profile after removal
                var profileAfterRemoval = healthProfileService.GetByAccountIdAsync(dto.AccountId).Result;

                // Assert: Links should be added and removed correctly
                return hasAllergy
                    && hasPreference
                    && !profileAfterRemoval.Allergies.Contains("Peanuts")
                    && !profileAfterRemoval.FoodPreferences.Contains("Vegetarian");
            });
    }

    /// <summary>
    /// Property 9: Positive weight and height validation
    /// For any health profile, weight and height must be positive numbers
    /// Validates: Requirements 2.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PositiveWeightAndHeightValidation()
    {
        return Prop.ForAll(
            GenerateValidHealthProfileDto(),
            Arb.From(Gen.Choose(-100, 0)),
            Arb.From(Gen.Choose(-100, 0)),
            (dto, negativeWeight, negativeHeight) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<HealthProfileService>>();
                var healthProfileService = new HealthProfileService(unitOfWork, mockLogger.Object, context);

                // Create account first
                var account = new Account
                {
                    Id = dto.AccountId,
                    Email = $"test{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Test User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Accounts.AddAsync(account).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Test negative weight
                var dtoWithNegativeWeight = new HealthProfileDto
                {
                    AccountId = dto.AccountId,
                    Age = dto.Age,
                    Weight = negativeWeight,
                    Height = dto.Height,
                    Gender = dto.Gender,
                    HealthNotes = dto.HealthNotes
                };

                bool weightValidationFailed = false;
                try
                {
                    healthProfileService.CreateOrUpdateAsync(dtoWithNegativeWeight).Wait();
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    weightValidationFailed = ex.Message.Contains("Weight must be a positive number");
                }

                // Test negative height
                var dtoWithNegativeHeight = new HealthProfileDto
                {
                    AccountId = dto.AccountId,
                    Age = dto.Age,
                    Weight = dto.Weight,
                    Height = negativeHeight,
                    Gender = dto.Gender,
                    HealthNotes = dto.HealthNotes
                };

                bool heightValidationFailed = false;
                try
                {
                    healthProfileService.CreateOrUpdateAsync(dtoWithNegativeHeight).Wait();
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    heightValidationFailed = ex.Message.Contains("Height must be a positive number");
                }

                // Assert: Both validations should fail
                return weightValidationFailed && heightValidationFailed;
            });
    }

    /// <summary>
    /// Property 10: Age range validation
    /// For any health profile, age must be between 1 and 150
    /// Validates: Requirements 2.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AgeRangeValidation()
    {
        return Prop.ForAll(
            GenerateValidHealthProfileDto(),
            Arb.From(Gen.Choose(-100, 0)),
            Arb.From(Gen.Choose(151, 300)),
            (dto, invalidAgeLow, invalidAgeHigh) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<HealthProfileService>>();
                var healthProfileService = new HealthProfileService(unitOfWork, mockLogger.Object, context);

                // Create account first
                var account = new Account
                {
                    Id = dto.AccountId,
                    Email = $"test{Guid.NewGuid()}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Test User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Accounts.AddAsync(account).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Test age too low
                var dtoWithLowAge = new HealthProfileDto
                {
                    AccountId = dto.AccountId,
                    Age = invalidAgeLow,
                    Weight = dto.Weight,
                    Height = dto.Height,
                    Gender = dto.Gender,
                    HealthNotes = dto.HealthNotes
                };

                bool lowAgeValidationFailed = false;
                try
                {
                    healthProfileService.CreateOrUpdateAsync(dtoWithLowAge).Wait();
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    lowAgeValidationFailed = ex.Message.Contains("Age must be between 1 and 150");
                }

                // Test age too high
                var dtoWithHighAge = new HealthProfileDto
                {
                    AccountId = dto.AccountId,
                    Age = invalidAgeHigh,
                    Weight = dto.Weight,
                    Height = dto.Height,
                    Gender = dto.Gender,
                    HealthNotes = dto.HealthNotes
                };

                bool highAgeValidationFailed = false;
                try
                {
                    healthProfileService.CreateOrUpdateAsync(dtoWithHighAge).Wait();
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    highAgeValidationFailed = ex.Message.Contains("Age must be between 1 and 150");
                }

                // Assert: Both validations should fail
                return lowAgeValidationFailed && highAgeValidationFailed;
            });
    }

    #region Generators

    /// <summary>
    /// Generator for valid HealthProfileDto
    /// </summary>
    private static Arbitrary<HealthProfileDto> GenerateValidHealthProfileDto()
    {
        var gen = from age in Gen.Choose(1, 150)
                  from weight in Gen.Choose(30, 200).Select(w => (float)w)
                  from height in Gen.Choose(100, 250).Select(h => (float)h)
                  from gender in Gen.Elements("Male", "Female", "Other")
                  from healthNotes in GenerateOptionalString(0, 200)
                  select new HealthProfileDto
                  {
                      AccountId = Guid.NewGuid(),
                      Age = age,
                      Weight = weight,
                      Height = height,
                      Gender = gender,
                      HealthNotes = healthNotes
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate an optional string (can be empty)
    /// </summary>
    private static Gen<string> GenerateOptionalString(int minLength, int maxLength)
    {
        return from length in Gen.Choose(minLength, maxLength)
               from chars in Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ".ToCharArray()))
               select new string(chars).Trim();
    }

    #endregion
}
