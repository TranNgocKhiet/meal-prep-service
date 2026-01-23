using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Tests;

/// <summary>
/// Property-based tests for repository operations
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class RepositoryPropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private MealPlanRepository _mealPlanRepository;
    private FridgeItemRepository _fridgeItemRepository;
    private Repository<Ingredient> _ingredientRepository;

    public RepositoryPropertyTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _mealPlanRepository = new MealPlanRepository(_context);
        _fridgeItemRepository = new FridgeItemRepository(_context);
        _ingredientRepository = new Repository<Ingredient>(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    /// <summary>
    /// Property 19: Meal plan account association
    /// For any meal plan created by a customer, the plan should be associated with that customer's account
    /// Validates: Requirements 4.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property MealPlanAccountAssociation()
    {
        return Prop.ForAll(
            GenerateValidMealPlanData(),
            data =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                var repository = new MealPlanRepository(context);

                // Create account first
                var account = new Account
                {
                    Id = data.AccountId,
                    Email = data.Email,
                    PasswordHash = "hashedpassword",
                    FullName = data.FullName,
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(account);
                context.SaveChanges();

                // Act: Create meal plan
                var mealPlan = new MealPlan
                {
                    Id = Guid.NewGuid(),
                    AccountId = data.AccountId,
                    PlanName = data.PlanName,
                    StartDate = data.StartDate,
                    EndDate = data.EndDate,
                    IsAiGenerated = data.IsAiGenerated
                };

                repository.AddAsync(mealPlan).Wait();
                context.SaveChanges();

                // Assert: Retrieve meal plan and verify account association
                var retrievedPlan = repository.GetByIdAsync(mealPlan.Id).Result;
                var accountPlans = repository.GetByAccountIdAsync(data.AccountId).Result;

                return retrievedPlan != null
                    && retrievedPlan.AccountId == data.AccountId
                    && accountPlans.Any(p => p.Id == mealPlan.Id);
            });
    }

    /// <summary>
    /// Property 30: Fridge item deletion
    /// For any fridge item, removing it should make it no longer retrievable from the fridge
    /// Validates: Requirements 7.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property FridgeItemDeletion()
    {
        return Prop.ForAll(
            GenerateValidFridgeItemData(),
            data =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                var repository = new FridgeItemRepository(context);

                // Create account first
                var account = new Account
                {
                    Id = data.AccountId,
                    Email = data.Email,
                    PasswordHash = "hashedpassword",
                    FullName = data.FullName,
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(account);

                // Create ingredient
                var ingredient = new Ingredient
                {
                    Id = data.IngredientId,
                    IngredientName = data.IngredientName,
                    Unit = data.Unit,
                    CaloPerUnit = data.CaloPerUnit,
                    IsAllergen = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.Ingredients.Add(ingredient);
                context.SaveChanges();

                // Act: Create fridge item
                var fridgeItem = new FridgeItem
                {
                    Id = Guid.NewGuid(),
                    AccountId = data.AccountId,
                    IngredientId = data.IngredientId,
                    CurrentAmount = data.CurrentAmount,
                    ExpiryDate = data.ExpiryDate
                };

                repository.AddAsync(fridgeItem).Wait();
                context.SaveChanges();

                var itemId = fridgeItem.Id;

                // Verify item exists before deletion
                var existsBeforeDeletion = repository.ExistsAsync(itemId).Result;

                // Delete the fridge item
                repository.DeleteAsync(itemId).Wait();
                context.SaveChanges();

                // Assert: Item should no longer be retrievable
                var retrievedAfterDeletion = repository.GetByIdAsync(itemId).Result;
                var existsAfterDeletion = repository.ExistsAsync(itemId).Result;

                return existsBeforeDeletion
                    && retrievedAfterDeletion == null
                    && !existsAfterDeletion;
            });
    }

    /// <summary>
    /// Property 54: Allergen flag storage
    /// For any ingredient created with is_allergen flag, the flag value should be stored and retrievable
    /// Validates: Requirements 11.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllergenFlagStorage()
    {
        return Prop.ForAll(
            GenerateValidIngredientData(),
            data =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                var repository = new Repository<Ingredient>(context);

                // Act: Create ingredient with allergen flag
                var ingredient = new Ingredient
                {
                    Id = Guid.NewGuid(),
                    IngredientName = data.IngredientName,
                    Unit = data.Unit,
                    CaloPerUnit = data.CaloPerUnit,
                    IsAllergen = data.IsAllergen
                };

                repository.AddAsync(ingredient).Wait();
                context.SaveChanges();

                // Assert: Retrieve ingredient and verify allergen flag is stored correctly
                var retrievedIngredient = repository.GetByIdAsync(ingredient.Id).Result;

                return retrievedIngredient != null
                    && retrievedIngredient.IsAllergen == data.IsAllergen
                    && retrievedIngredient.IngredientName == data.IngredientName
                    && retrievedIngredient.Unit == data.Unit
                    && Math.Abs(retrievedIngredient.CaloPerUnit - data.CaloPerUnit) < 0.01f;
            });
    }

    #region Generators

    /// <summary>
    /// Generator for valid meal plan test data
    /// </summary>
    private static Arbitrary<MealPlanTestData> GenerateValidMealPlanData()
    {
        var gen = from accountId in Arb.Generate<Guid>()
                  from email in GenerateValidEmail()
                  from fullName in GenerateNonEmptyString(3, 50)
                  from planName in GenerateNonEmptyString(3, 100)
                  from startDate in GenerateFutureDate()
                  from daysToAdd in Gen.Choose(1, 30)
                  from isAiGenerated in Arb.Generate<bool>()
                  select new MealPlanTestData
                  {
                      AccountId = accountId,
                      Email = email,
                      FullName = fullName,
                      PlanName = planName,
                      StartDate = startDate,
                      EndDate = startDate.AddDays(daysToAdd),
                      IsAiGenerated = isAiGenerated
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for valid fridge item test data
    /// </summary>
    private static Arbitrary<FridgeItemTestData> GenerateValidFridgeItemData()
    {
        var gen = from accountId in Arb.Generate<Guid>()
                  from email in GenerateValidEmail()
                  from fullName in GenerateNonEmptyString(3, 50)
                  from ingredientId in Arb.Generate<Guid>()
                  from ingredientName in GenerateNonEmptyString(3, 100)
                  from unit in GenerateUnit()
                  from caloPerUnit in GeneratePositiveFloat(1, 1000)
                  from currentAmount in GeneratePositiveFloat(0.1f, 1000)
                  from expiryDate in GenerateFutureDate()
                  select new FridgeItemTestData
                  {
                      AccountId = accountId,
                      Email = email,
                      FullName = fullName,
                      IngredientId = ingredientId,
                      IngredientName = ingredientName,
                      Unit = unit,
                      CaloPerUnit = caloPerUnit,
                      CurrentAmount = currentAmount,
                      ExpiryDate = expiryDate
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for valid ingredient test data
    /// </summary>
    private static Arbitrary<IngredientTestData> GenerateValidIngredientData()
    {
        var gen = from ingredientName in GenerateNonEmptyString(3, 100)
                  from unit in GenerateUnit()
                  from caloPerUnit in GeneratePositiveFloat(1, 1000)
                  from isAllergen in Arb.Generate<bool>()
                  select new IngredientTestData
                  {
                      IngredientName = ingredientName,
                      Unit = unit,
                      CaloPerUnit = caloPerUnit,
                      IsAllergen = isAllergen
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
               select $"{username}@{domain}.com";
    }

    /// <summary>
    /// Generate a non-empty string with specified length range
    /// </summary>
    private static Gen<string> GenerateNonEmptyString(int minLength, int maxLength)
    {
        return from length in Gen.Choose(minLength, maxLength)
               from chars in Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ".ToCharArray()))
               let str = new string(chars).Trim()
               where !string.IsNullOrWhiteSpace(str)
               select str.Length > maxLength ? str.Substring(0, maxLength) : str;
    }

    /// <summary>
    /// Generate a valid unit of measurement
    /// </summary>
    private static Gen<string> GenerateUnit()
    {
        return Gen.Elements("g", "kg", "ml", "l", "cup", "tbsp", "tsp", "oz", "lb", "piece");
    }

    /// <summary>
    /// Generate a positive float value
    /// </summary>
    private static Gen<float> GeneratePositiveFloat(float min, float max)
    {
        return from value in Gen.Choose((int)(min * 100), (int)(max * 100))
               select value / 100f;
    }

    /// <summary>
    /// Generate a future date
    /// </summary>
    private static Gen<DateTime> GenerateFutureDate()
    {
        return from daysFromNow in Gen.Choose(1, 365)
               select DateTime.UtcNow.Date.AddDays(daysFromNow);
    }

    #endregion

    #region Test Data Classes

    private class MealPlanTestData
    {
        public Guid AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAiGenerated { get; set; }
    }

    private class FridgeItemTestData
    {
        public Guid AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Guid IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float CaloPerUnit { get; set; }
        public float CurrentAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    private class IngredientTestData
    {
        public string IngredientName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public float CaloPerUnit { get; set; }
        public bool IsAllergen { get; set; }
    }

    #endregion
}
