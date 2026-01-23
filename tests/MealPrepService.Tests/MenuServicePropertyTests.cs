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
/// Property-based tests for MenuService
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class MenuServicePropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IMenuService _menuService;
    private Mock<ILogger<MenuService>> _mockLogger;

    public MenuServicePropertyTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockLogger = new Mock<ILogger<MenuService>>();
        _menuService = new MenuService(_unitOfWork, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _unitOfWork.Dispose();
    }

    /// <summary>
    /// Property 34: Menu creation with draft status
    /// For any valid future date, creating a daily menu should set status to "draft"
    /// Validates: Requirements 8.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MenuCreationWithDraftStatus()
    {
        return Prop.ForAll(
            GenerateFutureDate(),
            menuDate =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MenuService>>();
                var menuService = new MenuService(unitOfWork, mockLogger.Object);

                // Act: Create daily menu
                var result = menuService.CreateDailyMenuAsync(menuDate).Result;

                // Assert: Menu should be created with draft status
                return result != null
                    && result.Status == "draft"
                    && result.MenuDate.Date == menuDate.Date
                    && result.Id != Guid.Empty;
            });
    }

    /// <summary>
    /// Property 35: Menu meal required fields
    /// For any menu meal, adding it should require recipe ID, positive price, and non-negative quantity
    /// Validates: Requirements 8.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MenuMealRequiredFields()
    {
        return Prop.ForAll(
            GenerateValidMenuMealDto(),
            menuMealDto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MenuService>>();
                var menuService = new MenuService(unitOfWork, mockLogger.Object);

                // Create menu
                var menu = new DailyMenu
                {
                    Id = Guid.NewGuid(),
                    MenuDate = DateTime.UtcNow.AddDays(1),
                    Status = "draft",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.DailyMenus.AddAsync(menu).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Create recipe
                var recipe = new Recipe
                {
                    Id = menuMealDto.RecipeId,
                    RecipeName = "Test Recipe",
                    Instructions = "Test instructions",
                    TotalCalories = 500,
                    ProteinG = 20,
                    FatG = 10,
                    CarbsG = 50,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Recipes.AddAsync(recipe).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Act: Add meal to menu
                try
                {
                    menuService.AddMealToMenuAsync(menu.Id, menuMealDto).Wait();

                    // Assert: Should succeed with valid fields
                    return menuMealDto.RecipeId != Guid.Empty
                        && menuMealDto.Price > 0
                        && menuMealDto.AvailableQuantity >= 0;
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException)
                {
                    // Should fail if any required field is invalid
                    return menuMealDto.RecipeId == Guid.Empty
                        || menuMealDto.Price <= 0
                        || menuMealDto.AvailableQuantity < 0;
                }
            });
    }

    /// <summary>
    /// Property 36: Menu status transitions
    /// For any menu in draft status, publishing should transition status to "active"
    /// Validates: Requirements 8.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MenuStatusTransitions()
    {
        return Prop.ForAll(
            GenerateFutureDate(),
            menuDate =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MenuService>>();
                var menuService = new MenuService(unitOfWork, mockLogger.Object);

                // Create menu with draft status
                var menu = new DailyMenu
                {
                    Id = Guid.NewGuid(),
                    MenuDate = menuDate,
                    Status = "draft",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.DailyMenus.AddAsync(menu).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Create recipe and add meal to menu
                var recipe = new Recipe
                {
                    Id = Guid.NewGuid(),
                    RecipeName = "Test Recipe",
                    Instructions = "Test instructions",
                    TotalCalories = 500,
                    ProteinG = 20,
                    FatG = 10,
                    CarbsG = 50,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Recipes.AddAsync(recipe).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                var menuMeal = new MenuMeal
                {
                    Id = Guid.NewGuid(),
                    MenuId = menu.Id,
                    RecipeId = recipe.Id,
                    Price = 10.00m,
                    AvailableQuantity = 50,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.MenuMeals.AddAsync(menuMeal).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Act: Publish menu
                menuService.PublishMenuAsync(menu.Id).Wait();

                // Retrieve updated menu
                var updatedMenu = unitOfWork.DailyMenus.GetByIdAsync(menu.Id).Result;

                // Assert: Status should transition to active
                return updatedMenu != null
                    && updatedMenu.Status == "active";
            });
    }

    /// <summary>
    /// Property 37: Menu quantity validation
    /// For any menu meal, updating quantity should validate that quantity is non-negative
    /// Validates: Requirements 8.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MenuQuantityValidation()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(-1000, -1)),
            negativeQuantity =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MenuService>>();
                var menuService = new MenuService(unitOfWork, mockLogger.Object);

                // Create menu
                var menu = new DailyMenu
                {
                    Id = Guid.NewGuid(),
                    MenuDate = DateTime.UtcNow.AddDays(1),
                    Status = "draft",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.DailyMenus.AddAsync(menu).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Create recipe
                var recipe = new Recipe
                {
                    Id = Guid.NewGuid(),
                    RecipeName = "Test Recipe",
                    Instructions = "Test instructions",
                    TotalCalories = 500,
                    ProteinG = 20,
                    FatG = 10,
                    CarbsG = 50,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Recipes.AddAsync(recipe).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Create menu meal
                var menuMeal = new MenuMeal
                {
                    Id = Guid.NewGuid(),
                    MenuId = menu.Id,
                    RecipeId = recipe.Id,
                    Price = 10.00m,
                    AvailableQuantity = 50,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.MenuMeals.AddAsync(menuMeal).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Act & Assert: Try to update with negative quantity
                try
                {
                    menuService.UpdateMealQuantityAsync(menuMeal.Id, negativeQuantity).Wait();
                    return false; // Should not reach here
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    return ex.Message.Contains("cannot be negative");
                }
                catch
                {
                    return false; // Wrong exception type
                }
            });
    }

    /// <summary>
    /// Property 38: Weekly menu date range
    /// For any start date, weekly menu should return menus for the next 7 days
    /// Validates: Requirements 8.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property WeeklyMenuDateRange()
    {
        return Prop.ForAll(
            GenerateFutureDate(),
            startDate =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MenuService>>();
                var menuService = new MenuService(unitOfWork, mockLogger.Object);

                // Create menus for 7 days
                var menuDates = new List<DateTime>();
                for (int i = 0; i < 7; i++)
                {
                    var menuDate = startDate.AddDays(i);
                    menuDates.Add(menuDate);

                    var menu = new DailyMenu
                    {
                        Id = Guid.NewGuid(),
                        MenuDate = menuDate,
                        Status = "active",
                        CreatedAt = DateTime.UtcNow
                    };
                    unitOfWork.DailyMenus.AddAsync(menu).Wait();
                }
                unitOfWork.SaveChangesAsync().Wait();

                // Act: Get weekly menu
                var weeklyMenus = menuService.GetWeeklyMenuAsync(startDate).Result.ToList();

                // Assert: Should return menus for 7 days
                return weeklyMenus.Count == 7
                    && weeklyMenus.All(m => m.MenuDate >= startDate && m.MenuDate < startDate.AddDays(7))
                    && menuDates.All(date => weeklyMenus.Any(m => m.MenuDate.Date == date.Date));
            });
    }

    /// <summary>
    /// Property 39: Sold out status
    /// For any menu meal, when available quantity reaches zero, IsSoldOut should be true
    /// Validates: Requirements 8.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SoldOutStatus()
    {
        return Prop.ForAll(
            GenerateFutureDate(),
            menuDate =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MenuService>>();
                var menuService = new MenuService(unitOfWork, mockLogger.Object);

                // Create menu
                var menu = new DailyMenu
                {
                    Id = Guid.NewGuid(),
                    MenuDate = menuDate,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.DailyMenus.AddAsync(menu).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Create recipe
                var recipe = new Recipe
                {
                    Id = Guid.NewGuid(),
                    RecipeName = "Test Recipe",
                    Instructions = "Test instructions",
                    TotalCalories = 500,
                    ProteinG = 20,
                    FatG = 10,
                    CarbsG = 50,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Recipes.AddAsync(recipe).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Create menu meal with initial quantity
                var menuMeal = new MenuMeal
                {
                    Id = Guid.NewGuid(),
                    MenuId = menu.Id,
                    RecipeId = recipe.Id,
                    Price = 10.00m,
                    AvailableQuantity = 10,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.MenuMeals.AddAsync(menuMeal).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Act: Update quantity to zero
                menuService.UpdateMealQuantityAsync(menuMeal.Id, 0).Wait();

                // Retrieve menu with meals
                var retrievedMenu = unitOfWork.DailyMenus.GetWithMealsAsync(menu.Id).Result;
                var retrievedMeal = retrievedMenu?.MenuMeals.FirstOrDefault(m => m.Id == menuMeal.Id);

                // Get DTO to check IsSoldOut property
                var menuDto = menuService.GetByDateAsync(menuDate).Result;
                var mealDto = menuDto?.MenuMeals.FirstOrDefault(m => m.Id == menuMeal.Id);

                // Assert: IsSoldOut should be true when quantity is zero
                return retrievedMeal != null
                    && retrievedMeal.AvailableQuantity == 0
                    && mealDto != null
                    && mealDto.IsSoldOut == true;
            });
    }

    #region Generators

    /// <summary>
    /// Generator for future dates (1-30 days from now)
    /// </summary>
    private static Arbitrary<DateTime> GenerateFutureDate()
    {
        var gen = from days in Gen.Choose(1, 30)
                  select DateTime.UtcNow.Date.AddDays(days);

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for valid MenuMealDto
    /// </summary>
    private static Arbitrary<MenuMealDto> GenerateValidMenuMealDto()
    {
        var gen = from recipeId in Arb.Generate<Guid>()
                  from price in Gen.Choose(1, 100).Select(x => (decimal)x)
                  from quantity in Gen.Choose(0, 100)
                  select new MenuMealDto
                  {
                      RecipeId = recipeId,
                      Price = price,
                      AvailableQuantity = quantity
                  };

        return Arb.From(gen);
    }

    #endregion
}
