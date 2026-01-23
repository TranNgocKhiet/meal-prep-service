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
using Xunit;

namespace MealPrepService.Tests;

/// <summary>
/// Unit tests for MealPlanService DeleteAsync method
/// Tests error handling and logging requirements
/// </summary>
public class MealPlanServiceDeleteTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IMealPlanService _mealPlanService;
    private Mock<ILogger<MealPlanService>> _mockLogger;

    public MealPlanServiceDeleteTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockLogger = new Mock<ILogger<MealPlanService>>();
        var mockAIRecommendationService = new Mock<IAIRecommendationService>();
        _mealPlanService = new MealPlanService(_unitOfWork, _mockLogger.Object, mockAIRecommendationService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _unitOfWork.Dispose();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentMealPlan_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentPlanId = Guid.NewGuid();
        var requestingAccountId = Guid.NewGuid();

        // Create requesting account
        var account = new Account
        {
            Id = requestingAccountId,
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Test User",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _mealPlanService.DeleteAsync(nonExistentPlanId, requestingAccountId));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentRequestingAccount_ThrowsAuthenticationException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var nonExistentRequestingAccountId = Guid.NewGuid();

        // Create account and meal plan
        var account = new Account
        {
            Id = accountId,
            Email = "owner@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Owner",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(account);

        var mealPlan = new MealPlan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanName = "Test Plan",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            IsAiGenerated = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => _mealPlanService.DeleteAsync(mealPlan.Id, nonExistentRequestingAccountId));

        Assert.Contains("Requesting account not found", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_WithUnauthorizedUser_ThrowsAuthorizationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        // Create owner account
        var owner = new Account
        {
            Id = ownerId,
            Email = "owner@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Owner",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(owner);

        // Create other user account (not owner, not manager)
        var otherUser = new Account
        {
            Id = otherUserId,
            Email = "other@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Other User",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(otherUser);

        // Create meal plan owned by owner
        var mealPlan = new MealPlan
        {
            Id = Guid.NewGuid(),
            AccountId = ownerId,
            PlanName = "Owner's Plan",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            IsAiGenerated = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthorizationException>(
            () => _mealPlanService.DeleteAsync(mealPlan.Id, otherUserId));

        Assert.Contains("don't have permission", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_WithOwner_DeletesSuccessfullyAndLogs()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Create owner account
        var owner = new Account
        {
            Id = ownerId,
            Email = "owner@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Owner",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(owner);

        // Create meal plan
        var mealPlan = new MealPlan
        {
            Id = Guid.NewGuid(),
            AccountId = ownerId,
            PlanName = "Owner's Plan",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            IsAiGenerated = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync();

        // Act
        await _mealPlanService.DeleteAsync(mealPlan.Id, ownerId);

        // Assert - Meal plan should be deleted
        var deletedPlan = await _mealPlanService.GetByIdAsync(mealPlan.Id);
        Assert.Null(deletedPlan);

        // Assert - Logging should have occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deleted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithManager_DeletesSuccessfullyAndLogs()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        // Create owner account
        var owner = new Account
        {
            Id = ownerId,
            Email = "owner@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Owner",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(owner);

        // Create manager account
        var manager = new Account
        {
            Id = managerId,
            Email = "manager@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Manager",
            Role = "Manager",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(manager);

        // Create meal plan owned by owner
        var mealPlan = new MealPlan
        {
            Id = Guid.NewGuid(),
            AccountId = ownerId,
            PlanName = "Owner's Plan",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            IsAiGenerated = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync();

        // Act - Manager deletes owner's plan
        await _mealPlanService.DeleteAsync(mealPlan.Id, managerId);

        // Assert - Meal plan should be deleted
        var deletedPlan = await _mealPlanService.GetByIdAsync(mealPlan.Id);
        Assert.Null(deletedPlan);

        // Assert - Logging should have occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("deleted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CascadeDeletesMealsAndMealRecipes()
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Create owner account
        var owner = new Account
        {
            Id = ownerId,
            Email = "owner@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Owner",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _context.Accounts.Add(owner);

        // Create recipe
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            RecipeName = "Test Recipe",
            Instructions = "Test instructions",
            TotalCalories = 300,
            ProteinG = 20,
            FatG = 10,
            CarbsG = 30,
            CreatedAt = DateTime.UtcNow
        };
        _context.Recipes.Add(recipe);

        // Create meal plan
        var mealPlan = new MealPlan
        {
            Id = Guid.NewGuid(),
            AccountId = ownerId,
            PlanName = "Owner's Plan",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            IsAiGenerated = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.MealPlans.Add(mealPlan);

        // Create meal
        var meal = new Meal
        {
            Id = Guid.NewGuid(),
            PlanId = mealPlan.Id,
            MealType = "breakfast",
            ServeDate = DateTime.Today,
            CreatedAt = DateTime.UtcNow
        };
        _context.Meals.Add(meal);

        // Create meal-recipe link
        var mealRecipe = new MealRecipe
        {
            MealId = meal.Id,
            RecipeId = recipe.Id
        };
        _context.MealRecipes.Add(mealRecipe);

        await _context.SaveChangesAsync();

        // Act
        await _mealPlanService.DeleteAsync(mealPlan.Id, ownerId);

        // Assert - Meal plan should be deleted
        var deletedPlan = await _context.MealPlans.FindAsync(mealPlan.Id);
        Assert.Null(deletedPlan);

        // Assert - Meal should be cascade deleted
        var deletedMeal = await _context.Meals.FindAsync(meal.Id);
        Assert.Null(deletedMeal);

        // Assert - MealRecipe should be cascade deleted
        var deletedMealRecipe = await _context.MealRecipes
            .FirstOrDefaultAsync(mr => mr.MealId == meal.Id && mr.RecipeId == recipe.Id);
        Assert.Null(deletedMealRecipe);

        // Assert - Recipe should NOT be deleted
        var existingRecipe = await _context.Recipes.FindAsync(recipe.Id);
        Assert.NotNull(existingRecipe);
    }
}
