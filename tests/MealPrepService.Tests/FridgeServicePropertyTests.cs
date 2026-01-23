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
/// Property-based tests for FridgeService
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class FridgeServicePropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IFridgeService _fridgeService;
    private Mock<ILogger<FridgeService>> _mockLogger;

    public FridgeServicePropertyTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockLogger = new Mock<ILogger<FridgeService>>();
        _fridgeService = new FridgeService(_unitOfWork, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _unitOfWork.Dispose();
    }

    /// <summary>
    /// Property 28: Fridge item storage
    /// For any valid fridge item, adding it should store it with correct account and ingredient associations
    /// Validates: Requirements 7.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FridgeItemStorage()
    {
        return Prop.ForAll(
            GenerateValidFridgeItemDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<FridgeService>>();
                var fridgeService = new FridgeService(unitOfWork, mockLogger.Object);

                // Create test ingredient
                var ingredient = new Ingredient
                {
                    Id = dto.IngredientId,
                    IngredientName = "Test Ingredient",
                    Unit = "kg",
                    CaloPerUnit = 100,
                    IsAllergen = false,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Ingredients.AddAsync(ingredient).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Act: Add fridge item
                var result = fridgeService.AddItemAsync(dto).Result;

                // Retrieve the item
                var retrievedItems = fridgeService.GetFridgeItemsAsync(dto.AccountId).Result.ToList();

                // Assert: Item should be stored with correct associations
                return result != null
                    && result.AccountId == dto.AccountId
                    && result.IngredientId == dto.IngredientId
                    && result.CurrentAmount == dto.CurrentAmount
                    && result.ExpiryDate.Date == dto.ExpiryDate.Date
                    && retrievedItems.Any(i => i.Id == result.Id);
            });
    }

    /// <summary>
    /// Property 29: Non-negative quantity validation
    /// For any negative quantity, updating fridge item quantity should throw BusinessException
    /// Validates: Requirements 7.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NonNegativeQuantityValidation()
    {
        return Prop.ForAll(
            GenerateValidFridgeItemDto(),
            Arb.From(Gen.Choose(-1000, -1).Select(x => (float)x)),
            (dto, negativeQuantity) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<FridgeService>>();
                var fridgeService = new FridgeService(unitOfWork, mockLogger.Object);

                // Create test ingredient
                var ingredient = new Ingredient
                {
                    Id = dto.IngredientId,
                    IngredientName = "Test Ingredient",
                    Unit = "kg",
                    CaloPerUnit = 100,
                    IsAllergen = false,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Ingredients.AddAsync(ingredient).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Add fridge item
                var addedItem = fridgeService.AddItemAsync(dto).Result;

                // Act & Assert: Try to update with negative quantity
                try
                {
                    fridgeService.UpdateItemQuantityAsync(addedItem.Id, negativeQuantity).Wait();
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
    /// Property 31: Fridge item retrieval
    /// For any account, retrieving fridge items should return all items belonging to that account
    /// Validates: Requirements 7.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FridgeItemRetrieval()
    {
        return Prop.ForAll(
            GenerateAccountId(),
            Arb.From(GenerateValidFridgeItemDtoList(3)),
            (accountId, dtoList) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<FridgeService>>();
                var fridgeService = new FridgeService(unitOfWork, mockLogger.Object);

                // Create test ingredients and add fridge items
                var addedItemIds = new List<Guid>();
                foreach (var dto in dtoList)
                {
                    dto.AccountId = accountId; // Set same account for all items

                    var ingredient = new Ingredient
                    {
                        Id = dto.IngredientId,
                        IngredientName = $"Ingredient {dto.IngredientId}",
                        Unit = "kg",
                        CaloPerUnit = 100,
                        IsAllergen = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    unitOfWork.Ingredients.AddAsync(ingredient).Wait();
                    unitOfWork.SaveChangesAsync().Wait();

                    var addedItem = fridgeService.AddItemAsync(dto).Result;
                    addedItemIds.Add(addedItem.Id);
                }

                // Act: Retrieve fridge items for the account
                var retrievedItems = fridgeService.GetFridgeItemsAsync(accountId).Result.ToList();

                // Assert: All added items should be retrieved
                return retrievedItems.Count == dtoList.Count
                    && addedItemIds.All(id => retrievedItems.Any(item => item.Id == id))
                    && retrievedItems.All(item => item.AccountId == accountId);
            });
    }

    /// <summary>
    /// Property 32: Grocery list generation
    /// For any meal plan, grocery list should contain ingredients where required amount exceeds current fridge amount
    /// Validates: Requirements 7.5, 7.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property GroceryListGeneration()
    {
        return Prop.ForAll(
            GenerateAccountId(),
            accountId =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<FridgeService>>();
                var fridgeService = new FridgeService(unitOfWork, mockLogger.Object);

                // Create test ingredient
                var ingredient = new Ingredient
                {
                    Id = Guid.NewGuid(),
                    IngredientName = "Test Ingredient",
                    Unit = "kg",
                    CaloPerUnit = 100,
                    IsAllergen = false,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Ingredients.AddAsync(ingredient).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Add fridge item with low quantity
                var fridgeDto = new FridgeItemDto
                {
                    AccountId = accountId,
                    IngredientId = ingredient.Id,
                    CurrentAmount = 1.0f,
                    ExpiryDate = DateTime.UtcNow.AddDays(10)
                };
                fridgeService.AddItemAsync(fridgeDto).Wait();

                // Create meal plan with recipe requiring more of the ingredient
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

                var recipeIngredient = new RecipeIngredient
                {
                    RecipeId = recipe.Id,
                    IngredientId = ingredient.Id,
                    Amount = 5.0f // Requires more than fridge has
                };
                context.Set<RecipeIngredient>().Add(recipeIngredient);
                context.SaveChanges();

                var mealPlan = new MealPlan
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    PlanName = "Test Plan",
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(7),
                    IsAiGenerated = false,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.MealPlans.AddAsync(mealPlan).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                var meal = new Meal
                {
                    Id = Guid.NewGuid(),
                    PlanId = mealPlan.Id,
                    MealType = "Lunch",
                    ServeDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                context.Set<Meal>().Add(meal);
                context.SaveChanges();

                var mealRecipe = new MealRecipe
                {
                    MealId = meal.Id,
                    RecipeId = recipe.Id
                };
                context.Set<MealRecipe>().Add(mealRecipe);
                context.SaveChanges();

                // Act: Generate grocery list
                var groceryList = fridgeService.GenerateGroceryListAsync(accountId, mealPlan.Id).Result;

                // Assert: Grocery list should contain the ingredient with needed amount
                return groceryList != null
                    && groceryList.AccountId == accountId
                    && groceryList.MealPlanId == mealPlan.Id
                    && groceryList.MissingIngredients.Any(item => 
                        item.IngredientId == ingredient.Id 
                        && item.RequiredAmount == 5.0f
                        && item.CurrentAmount == 1.0f
                        && item.NeededAmount == 4.0f);
            });
    }

    /// <summary>
    /// Property 33: Expiry status determination
    /// For any fridge item, expiry status should be correctly determined based on expiry date
    /// Validates: Requirements 7.6, 7.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ExpiryStatusDetermination()
    {
        return Prop.ForAll(
            GenerateValidFridgeItemDto(),
            Arb.From(Gen.Choose(1, 3)), // Days until expiry (within expiring threshold)
            (dto, daysUntilExpiry) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<FridgeService>>();
                var fridgeService = new FridgeService(unitOfWork, mockLogger.Object);

                // Create test ingredient
                var ingredient = new Ingredient
                {
                    Id = dto.IngredientId,
                    IngredientName = "Test Ingredient",
                    Unit = "kg",
                    CaloPerUnit = 100,
                    IsAllergen = false,
                    CreatedAt = DateTime.UtcNow
                };
                unitOfWork.Ingredients.AddAsync(ingredient).Wait();
                unitOfWork.SaveChangesAsync().Wait();

                // Set expiry date to be within expiring threshold
                dto.ExpiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry);

                // Act: Add fridge item
                var addedItem = fridgeService.AddItemAsync(dto).Result;

                // Retrieve expiring items
                var expiringItems = fridgeService.GetExpiringItemsAsync(dto.AccountId).Result.ToList();

                // Assert: Item should be marked as expiring
                return addedItem.IsExpiring == true
                    && addedItem.IsExpired == false
                    && expiringItems.Any(item => item.Id == addedItem.Id);
            });
    }

    #region Generators

    /// <summary>
    /// Generator for valid FridgeItemDto
    /// </summary>
    private static Arbitrary<FridgeItemDto> GenerateValidFridgeItemDto()
    {
        var gen = from accountId in Arb.Generate<Guid>()
                  from ingredientId in Arb.Generate<Guid>()
                  from amount in Gen.Choose(1, 100).Select(x => (float)x)
                  from daysUntilExpiry in Gen.Choose(4, 30) // Future date beyond expiring threshold
                  select new FridgeItemDto
                  {
                      AccountId = accountId,
                      IngredientId = ingredientId,
                      CurrentAmount = amount,
                      ExpiryDate = DateTime.UtcNow.AddDays(daysUntilExpiry)
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for a list of valid FridgeItemDto
    /// </summary>
    private static Gen<List<FridgeItemDto>> GenerateValidFridgeItemDtoList(int count)
    {
        return Gen.ListOf(count, GenerateValidFridgeItemDto().Generator)
            .Select(fsList => new List<FridgeItemDto>(fsList));
    }

    /// <summary>
    /// Generator for account ID
    /// </summary>
    private static Arbitrary<Guid> GenerateAccountId()
    {
        return Arb.From(Arb.Generate<Guid>());
    }

    #endregion
}