using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Tests;

/// <summary>
/// Property-based tests for MealPlanService deletion functionality
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class MealPlanServicePropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IMealPlanService _mealPlanService;
    private Mock<ILogger<MealPlanService>> _mockLogger;

    public MealPlanServicePropertyTests()
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

    /// <summary>
    /// Property 1: Deletion completeness
    /// For any meal plan with associated meals and meal-recipe relationships,
    /// after successful deletion, the meal plan and all associated meals are removed from the database.
    /// **Validates: Requirements US-3.1, US-3.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property DeletionCompleteness()
    {
        return Prop.ForAll(
            GenerateValidMealPlanWithMeals(),
            testData =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MealPlanService>>();
                var mockAIRecommendationService = new Mock<IAIRecommendationService>();
                var mealPlanService = new MealPlanService(unitOfWork, mockLogger.Object, mockAIRecommendationService.Object);

                // Create account
                var account = new Account
                {
                    Id = testData.AccountId,
                    Email = $"test{testData.AccountId}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Test User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(account);
                context.SaveChanges();

                // Create recipes
                var recipeIds = new List<Guid>();
                foreach (var recipeId in testData.RecipeIds)
                {
                    var recipe = new Recipe
                    {
                        Id = recipeId,
                        RecipeName = $"Recipe {recipeId}",
                        Instructions = "Test instructions",
                        TotalCalories = 300,
                        ProteinG = 20,
                        FatG = 10,
                        CarbsG = 30,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Recipes.Add(recipe);
                    recipeIds.Add(recipeId);
                }
                context.SaveChanges();

                // Create meal plan
                var mealPlan = new MealPlan
                {
                    Id = testData.MealPlanId,
                    AccountId = testData.AccountId,
                    PlanName = testData.PlanName,
                    StartDate = testData.StartDate,
                    EndDate = testData.EndDate,
                    IsAiGenerated = testData.IsAiGenerated,
                    CreatedAt = DateTime.UtcNow
                };
                context.MealPlans.Add(mealPlan);
                context.SaveChanges();

                // Create meals and meal-recipe relationships
                var mealIds = new List<Guid>();
                var mealRecipeCount = 0;
                for (int i = 0; i < testData.MealCount; i++)
                {
                    var mealId = Guid.NewGuid();
                    var meal = new Meal
                    {
                        Id = mealId,
                        PlanId = testData.MealPlanId,
                        MealType = testData.MealTypes[i % testData.MealTypes.Count],
                        ServeDate = testData.StartDate.AddDays(i),
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Meals.Add(meal);
                    mealIds.Add(mealId);

                    // Add meal-recipe relationships (1-2 recipes per meal)
                    var recipesPerMeal = Math.Min(1 + (i % 2), recipeIds.Count);
                    for (int j = 0; j < recipesPerMeal; j++)
                    {
                        var mealRecipe = new MealRecipe
                        {
                            MealId = mealId,
                            RecipeId = recipeIds[j % recipeIds.Count]
                        };
                        context.MealRecipes.Add(mealRecipe);
                        mealRecipeCount++;
                    }
                }
                context.SaveChanges();

                // Verify setup: meal plan, meals, and meal-recipes exist
                var mealPlanBeforeDelete = context.MealPlans.Find(testData.MealPlanId);
                var mealsBeforeDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanId).ToList();
                var mealRecipesBeforeDelete = context.MealRecipes
                    .Where(mr => mealIds.Contains(mr.MealId))
                    .ToList();

                if (mealPlanBeforeDelete == null || 
                    mealsBeforeDelete.Count != testData.MealCount ||
                    mealRecipesBeforeDelete.Count != mealRecipeCount)
                {
                    return false; // Setup failed
                }

                // Act: Delete the meal plan
                mealPlanService.DeleteAsync(testData.MealPlanId, testData.AccountId).Wait();

                // Assert: Verify deletion completeness
                // Property 1: Meal plan should be removed
                var mealPlanAfterDelete = context.MealPlans.Find(testData.MealPlanId);
                var mealPlanDeleted = mealPlanAfterDelete == null;

                // Property 2: All associated meals should be removed
                var mealsAfterDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanId).ToList();
                var allMealsDeleted = mealsAfterDelete.Count == 0;

                // Property 3: All meal-recipe relationships should be removed
                var mealRecipesAfterDelete = context.MealRecipes
                    .Where(mr => mealIds.Contains(mr.MealId))
                    .ToList();
                var allMealRecipesDeleted = mealRecipesAfterDelete.Count == 0;

                // Property 4: Recipes should NOT be deleted (they may be used in other meal plans)
                var recipesAfterDelete = context.Recipes
                    .Where(r => recipeIds.Contains(r.Id))
                    .ToList();
                var recipesNotDeleted = recipesAfterDelete.Count == recipeIds.Count;

                // Property 5: GetByIdAsync should return null for deleted meal plan
                var retrievedMealPlan = mealPlanService.GetByIdAsync(testData.MealPlanId).Result;
                var getByIdReturnsNull = retrievedMealPlan == null;

                return mealPlanDeleted
                    && allMealsDeleted
                    && allMealRecipesDeleted
                    && recipesNotDeleted
                    && getByIdReturnsNull;
            });
    }

    /// <summary>
    /// Property 2: Cascade integrity - Recipes not deleted
    /// For any meal plan deletion with associated recipes,
    /// the recipes themselves should remain in the database (they may be used in other meal plans).
    /// Only the meal plan, meals, and meal-recipe relationships should be deleted.
    /// **Validates: Requirements US-3.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CascadeIntegrityRecipesNotDeleted()
    {
        return Prop.ForAll(
            GenerateMealPlanWithSharedRecipes(),
            testData =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MealPlanService>>();
                var mockAIRecommendationService = new Mock<IAIRecommendationService>();
                var mealPlanService = new MealPlanService(unitOfWork, mockLogger.Object, mockAIRecommendationService.Object);

                // Create account
                var account = new Account
                {
                    Id = testData.AccountId,
                    Email = $"test{testData.AccountId}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Test User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(account);
                context.SaveChanges();

                // Create recipes that will be shared across meal plans
                var recipes = new List<Recipe>();
                foreach (var recipeId in testData.RecipeIds)
                {
                    var recipe = new Recipe
                    {
                        Id = recipeId,
                        RecipeName = $"Recipe {recipeId}",
                        Instructions = "Test instructions",
                        TotalCalories = 300,
                        ProteinG = 20,
                        FatG = 10,
                        CarbsG = 30,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Recipes.Add(recipe);
                    recipes.Add(recipe);
                }
                context.SaveChanges();

                // Create the meal plan to be deleted
                var mealPlanToDelete = new MealPlan
                {
                    Id = testData.MealPlanIdToDelete,
                    AccountId = testData.AccountId,
                    PlanName = "Plan To Delete",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(7),
                    IsAiGenerated = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.MealPlans.Add(mealPlanToDelete);
                context.SaveChanges();

                // Create meals for the meal plan to be deleted
                var mealsToDelete = new List<Meal>();
                for (int i = 0; i < testData.MealCount; i++)
                {
                    var meal = new Meal
                    {
                        Id = Guid.NewGuid(),
                        PlanId = testData.MealPlanIdToDelete,
                        MealType = testData.MealTypes[i % testData.MealTypes.Count],
                        ServeDate = DateTime.Today.AddDays(i),
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Meals.Add(meal);
                    mealsToDelete.Add(meal);

                    // Link recipes to meals (each meal uses 1-2 recipes)
                    var recipesPerMeal = Math.Min(1 + (i % 2), recipes.Count);
                    for (int j = 0; j < recipesPerMeal; j++)
                    {
                        var mealRecipe = new MealRecipe
                        {
                            MealId = meal.Id,
                            RecipeId = recipes[j % recipes.Count].Id
                        };
                        context.MealRecipes.Add(mealRecipe);
                    }
                }
                context.SaveChanges();

                // Create another meal plan that uses the SAME recipes (to prove they're shared)
                var otherMealPlan = new MealPlan
                {
                    Id = testData.OtherMealPlanId,
                    AccountId = testData.AccountId,
                    PlanName = "Other Plan",
                    StartDate = DateTime.Today.AddDays(14),
                    EndDate = DateTime.Today.AddDays(21),
                    IsAiGenerated = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.MealPlans.Add(otherMealPlan);
                context.SaveChanges();

                // Create meals for the other meal plan using the same recipes
                var otherMeals = new List<Meal>();
                for (int i = 0; i < 2; i++) // Just 2 meals for the other plan
                {
                    var meal = new Meal
                    {
                        Id = Guid.NewGuid(),
                        PlanId = testData.OtherMealPlanId,
                        MealType = "lunch",
                        ServeDate = DateTime.Today.AddDays(14 + i),
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Meals.Add(meal);
                    otherMeals.Add(meal);

                    // Link the same recipes to this meal plan
                    var mealRecipe = new MealRecipe
                    {
                        MealId = meal.Id,
                        RecipeId = recipes[i % recipes.Count].Id
                    };
                    context.MealRecipes.Add(mealRecipe);
                }
                context.SaveChanges();

                // Verify setup: all entities exist before deletion
                var recipesBeforeDelete = context.Recipes.Where(r => testData.RecipeIds.Contains(r.Id)).ToList();
                var mealPlanBeforeDelete = context.MealPlans.Find(testData.MealPlanIdToDelete);
                var mealsBeforeDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanIdToDelete).ToList();
                var otherMealPlanBeforeDelete = context.MealPlans.Find(testData.OtherMealPlanId);
                var otherMealsBeforeDelete = context.Meals.Where(m => m.PlanId == testData.OtherMealPlanId).ToList();

                if (recipesBeforeDelete.Count != testData.RecipeIds.Count ||
                    mealPlanBeforeDelete == null ||
                    mealsBeforeDelete.Count != testData.MealCount ||
                    otherMealPlanBeforeDelete == null ||
                    otherMealsBeforeDelete.Count != 2)
                {
                    return false; // Setup failed
                }

                // Act: Delete the first meal plan
                mealPlanService.DeleteAsync(testData.MealPlanIdToDelete, testData.AccountId).Wait();

                // Assert: Verify cascade integrity properties
                
                // Property 1: The deleted meal plan should be removed
                var mealPlanAfterDelete = context.MealPlans.Find(testData.MealPlanIdToDelete);
                var mealPlanDeleted = mealPlanAfterDelete == null;

                // Property 2: All meals from the deleted plan should be removed
                var mealsAfterDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanIdToDelete).ToList();
                var allMealsDeleted = mealsAfterDelete.Count == 0;

                // Property 3: Meal-recipe relationships for deleted meals should be removed
                var mealIdsToDelete = mealsToDelete.Select(m => m.Id).ToList();
                var mealRecipesAfterDelete = context.MealRecipes
                    .Where(mr => mealIdsToDelete.Contains(mr.MealId))
                    .ToList();
                var allMealRecipesDeleted = mealRecipesAfterDelete.Count == 0;

                // Property 4: RECIPES SHOULD NOT BE DELETED (they may be used in other meal plans)
                var recipesAfterDelete = context.Recipes.Where(r => testData.RecipeIds.Contains(r.Id)).ToList();
                var allRecipesStillExist = recipesAfterDelete.Count == testData.RecipeIds.Count;

                // Property 5: The other meal plan should still exist
                var otherMealPlanAfterDelete = context.MealPlans.Find(testData.OtherMealPlanId);
                var otherMealPlanStillExists = otherMealPlanAfterDelete != null;

                // Property 6: The other meal plan's meals should still exist
                var otherMealsAfterDelete = context.Meals.Where(m => m.PlanId == testData.OtherMealPlanId).ToList();
                var otherMealsStillExist = otherMealsAfterDelete.Count == 2;

                // Property 7: The other meal plan's meal-recipe relationships should still exist
                var otherMealIds = otherMeals.Select(m => m.Id).ToList();
                var otherMealRecipesAfterDelete = context.MealRecipes
                    .Where(mr => otherMealIds.Contains(mr.MealId))
                    .ToList();
                var otherMealRecipesStillExist = otherMealRecipesAfterDelete.Count == 2;

                // All properties must hold for cascade integrity
                return mealPlanDeleted
                    && allMealsDeleted
                    && allMealRecipesDeleted
                    && allRecipesStillExist // KEY PROPERTY: Recipes not deleted
                    && otherMealPlanStillExists
                    && otherMealsStillExist
                    && otherMealRecipesStillExist;
            });
    }

    /// <summary>
    /// Property 3: Authorization enforcement
    /// For any deletion attempt, only the owner or a manager can delete the meal plan.
    /// Unauthorized users should receive an AuthorizationException and the meal plan should remain.
    /// **Validates: Requirements US-1.5, BR-1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AuthorizationEnforcement()
    {
        return Prop.ForAll(
            GenerateAuthorizationTestData(),
            testData =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MealPlanService>>();
                var mockAIRecommendationService = new Mock<IAIRecommendationService>();
                var mealPlanService = new MealPlanService(unitOfWork, mockLogger.Object, mockAIRecommendationService.Object);

                // Create owner account
                var ownerAccount = new Account
                {
                    Id = testData.OwnerId,
                    Email = $"owner{testData.OwnerId}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Owner User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(ownerAccount);

                // Create requester account with specified role (only if different from owner)
                if (!testData.IsOwner)
                {
                    var requesterAccount = new Account
                    {
                        Id = testData.RequesterId,
                        Email = $"requester{testData.RequesterId}@example.com",
                        PasswordHash = "hashedpassword",
                        FullName = "Requester User",
                        Role = testData.RequesterRole,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Accounts.Add(requesterAccount);
                }

                // Create meal plan owned by owner
                var mealPlan = new MealPlan
                {
                    Id = testData.MealPlanId,
                    AccountId = testData.OwnerId,
                    PlanName = testData.PlanName,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(7),
                    IsAiGenerated = false,
                    CreatedAt = DateTime.UtcNow
                };
                context.MealPlans.Add(mealPlan);

                // Add a meal to verify cascade behavior
                var meal = new Meal
                {
                    Id = Guid.NewGuid(),
                    PlanId = testData.MealPlanId,
                    MealType = "lunch",
                    ServeDate = DateTime.Today,
                    CreatedAt = DateTime.UtcNow
                };
                context.Meals.Add(meal);
                context.SaveChanges();

                // Determine expected behavior based on authorization
                bool shouldSucceed = testData.IsOwner || testData.RequesterRole == "Manager";

                try
                {
                    // Act: Attempt to delete the meal plan
                    mealPlanService.DeleteAsync(testData.MealPlanId, testData.RequesterId).Wait();

                    // If we reach here, deletion succeeded
                    if (!shouldSucceed)
                    {
                        // Deletion should have failed but didn't - property violated
                        return false;
                    }

                    // Verify deletion was successful
                    var mealPlanAfterDelete = context.MealPlans.Find(testData.MealPlanId);
                    var mealsAfterDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanId).ToList();

                    // Property: Authorized deletion should remove meal plan and meals
                    return mealPlanAfterDelete == null && mealsAfterDelete.Count == 0;
                }
                catch (AggregateException ex) when (ex.InnerException is AuthorizationException)
                {
                    // Authorization exception was thrown
                    if (shouldSucceed)
                    {
                        // Deletion should have succeeded but failed - property violated
                        return false;
                    }

                    // Verify meal plan still exists after failed authorization
                    var mealPlanAfterFailedDelete = context.MealPlans.Find(testData.MealPlanId);
                    var mealsAfterFailedDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanId).ToList();

                    // Property: Failed authorization should leave meal plan and meals intact
                    return mealPlanAfterFailedDelete != null && mealsAfterFailedDelete.Count == 1;
                }
                catch (Exception)
                {
                    // Unexpected exception - property violated
                    return false;
                }
            });
    }

    #region Generators

    /// <summary>
    /// Generator for valid meal plan test data with meals
    /// </summary>
    private static Arbitrary<MealPlanTestData> GenerateValidMealPlanWithMeals()
    {
        var gen = from accountId in Arb.Generate<Guid>()
                  from mealPlanId in Arb.Generate<Guid>()
                  from planName in GenerateNonEmptyString(5, 50)
                  from daysInPlan in Gen.Choose(1, 14) // 1-14 days
                  from mealCount in Gen.Choose(1, 10) // 1-10 meals
                  from recipeCount in Gen.Choose(1, 5) // 1-5 recipes
                  from recipeIds in Gen.ListOf(recipeCount, Arb.Generate<Guid>())
                  from isAiGenerated in Arb.Generate<bool>()
                  from mealTypes in Gen.ListOf(mealCount, Gen.Elements("breakfast", "lunch", "dinner", "snack"))
                  let startDate = DateTime.Today
                  let endDate = startDate.AddDays(daysInPlan)
                  select new MealPlanTestData
                  {
                      AccountId = accountId,
                      MealPlanId = mealPlanId,
                      PlanName = planName,
                      StartDate = startDate,
                      EndDate = endDate,
                      IsAiGenerated = isAiGenerated,
                      MealCount = mealCount,
                      MealTypes = mealTypes.ToList(),
                      RecipeIds = recipeIds.ToList()
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate a non-empty string with specified length range
    /// </summary>
    private static Gen<string> GenerateNonEmptyString(int minLength, int maxLength)
    {
        return from length in Gen.Choose(minLength, maxLength)
               from chars in Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ".ToCharArray()))
               let str = new string(chars).Trim()
               where !string.IsNullOrWhiteSpace(str) && str.Length >= minLength
               select str.Length > maxLength ? str.Substring(0, maxLength) : str;
    }

    /// <summary>
    /// Generator for meal plan with shared recipes test data
    /// Creates a scenario where recipes are used by multiple meal plans
    /// </summary>
    private static Arbitrary<MealPlanWithSharedRecipesTestData> GenerateMealPlanWithSharedRecipes()
    {
        var gen = from accountId in Arb.Generate<Guid>()
                  from mealPlanIdToDelete in Arb.Generate<Guid>()
                  from otherMealPlanId in Arb.Generate<Guid>()
                  from mealCount in Gen.Choose(1, 8) // 1-8 meals
                  from recipeCount in Gen.Choose(2, 5) // 2-5 recipes (ensure multiple recipes)
                  from recipeIds in Gen.ListOf(recipeCount, Arb.Generate<Guid>())
                  from mealTypes in Gen.ListOf(mealCount, Gen.Elements("breakfast", "lunch", "dinner", "snack"))
                  select new MealPlanWithSharedRecipesTestData
                  {
                      AccountId = accountId,
                      MealPlanIdToDelete = mealPlanIdToDelete,
                      OtherMealPlanId = otherMealPlanId,
                      MealCount = mealCount,
                      MealTypes = mealTypes.ToList(),
                      RecipeIds = recipeIds.Distinct().ToList() // Ensure unique recipe IDs
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for authorization test data
    /// Generates scenarios with different owner/requester combinations and roles
    /// </summary>
    private static Arbitrary<AuthorizationTestData> GenerateAuthorizationTestData()
    {
        var gen = from ownerId in Arb.Generate<Guid>()
                  from requesterId in Arb.Generate<Guid>()
                  from mealPlanId in Arb.Generate<Guid>()
                  from planName in GenerateNonEmptyString(5, 50)
                  from isOwner in Arb.Generate<bool>()
                  from requesterRole in Gen.Elements("Customer", "Manager")
                  let actualRequesterId = isOwner ? ownerId : requesterId
                  select new AuthorizationTestData
                  {
                      OwnerId = ownerId,
                      RequesterId = actualRequesterId,
                      MealPlanId = mealPlanId,
                      PlanName = planName,
                      IsOwner = isOwner,
                      RequesterRole = requesterRole
                  };

        return Arb.From(gen);
    }

    #endregion

    #region Test Data Classes

    /// <summary>
    /// Test data for meal plan with meals
    /// </summary>
    private class MealPlanTestData
    {
        public Guid AccountId { get; set; }
        public Guid MealPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAiGenerated { get; set; }
        public int MealCount { get; set; }
        public List<string> MealTypes { get; set; } = new();
        public List<Guid> RecipeIds { get; set; } = new();
    }

    /// <summary>
    /// Test data for meal plan with shared recipes
    /// </summary>
    private class MealPlanWithSharedRecipesTestData
    {
        public Guid AccountId { get; set; }
        public Guid MealPlanIdToDelete { get; set; }
        public Guid OtherMealPlanId { get; set; }
        public int MealCount { get; set; }
        public List<string> MealTypes { get; set; } = new();
        public List<Guid> RecipeIds { get; set; } = new();
    }

    /// <summary>
    /// Test data for authorization scenarios
    /// </summary>
    private class AuthorizationTestData
    {
        public Guid OwnerId { get; set; }
        public Guid RequesterId { get; set; }
        public Guid MealPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public string RequesterRole { get; set; } = string.Empty;
    }

    #endregion

    /// <summary>
    /// Property 4: Transactional atomicity
    /// For any deletion operation, either all related records are deleted or none are deleted.
    /// The database should never be left in a partial deletion state.
    /// **Validates: Requirements NFR-3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property TransactionalAtomicity()
    {
        return Prop.ForAll(
            GenerateValidMealPlanWithMeals(),
            testData =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<MealPlanService>>();
                var mockAIRecommendationService = new Mock<IAIRecommendationService>();
                var mealPlanService = new MealPlanService(unitOfWork, mockLogger.Object, mockAIRecommendationService.Object);

                // Create account
                var account = new Account
                {
                    Id = testData.AccountId,
                    Email = $"test{testData.AccountId}@example.com",
                    PasswordHash = "hashedpassword",
                    FullName = "Test User",
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                context.Accounts.Add(account);
                context.SaveChanges();

                // Create recipes
                var recipeIds = new List<Guid>();
                foreach (var recipeId in testData.RecipeIds)
                {
                    var recipe = new Recipe
                    {
                        Id = recipeId,
                        RecipeName = $"Recipe {recipeId}",
                        Instructions = "Test instructions",
                        TotalCalories = 300,
                        ProteinG = 20,
                        FatG = 10,
                        CarbsG = 30,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Recipes.Add(recipe);
                    recipeIds.Add(recipeId);
                }
                context.SaveChanges();

                // Create meal plan
                var mealPlan = new MealPlan
                {
                    Id = testData.MealPlanId,
                    AccountId = testData.AccountId,
                    PlanName = testData.PlanName,
                    StartDate = testData.StartDate,
                    EndDate = testData.EndDate,
                    IsAiGenerated = testData.IsAiGenerated,
                    CreatedAt = DateTime.UtcNow
                };
                context.MealPlans.Add(mealPlan);
                context.SaveChanges();

                // Create meals and meal-recipe relationships
                var mealIds = new List<Guid>();
                var mealRecipeCount = 0;
                for (int i = 0; i < testData.MealCount; i++)
                {
                    var mealId = Guid.NewGuid();
                    var meal = new Meal
                    {
                        Id = mealId,
                        PlanId = testData.MealPlanId,
                        MealType = testData.MealTypes[i % testData.MealTypes.Count],
                        ServeDate = testData.StartDate.AddDays(i),
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Meals.Add(meal);
                    mealIds.Add(mealId);

                    // Add meal-recipe relationships (1-2 recipes per meal)
                    var recipesPerMeal = Math.Min(1 + (i % 2), recipeIds.Count);
                    for (int j = 0; j < recipesPerMeal; j++)
                    {
                        var mealRecipe = new MealRecipe
                        {
                            MealId = mealId,
                            RecipeId = recipeIds[j % recipeIds.Count]
                        };
                        context.MealRecipes.Add(mealRecipe);
                        mealRecipeCount++;
                    }
                }
                context.SaveChanges();

                // Record initial counts before deletion
                var initialMealPlanCount = context.MealPlans.Count();
                var initialMealCount = context.Meals.Count();
                var initialMealRecipeCount = context.MealRecipes.Count();
                var initialRecipeCount = context.Recipes.Count();

                // Verify setup: all entities exist
                var mealPlanBeforeDelete = context.MealPlans.Find(testData.MealPlanId);
                var mealsBeforeDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanId).ToList();
                var mealRecipesBeforeDelete = context.MealRecipes
                    .Where(mr => mealIds.Contains(mr.MealId))
                    .ToList();

                if (mealPlanBeforeDelete == null || 
                    mealsBeforeDelete.Count != testData.MealCount ||
                    mealRecipesBeforeDelete.Count != mealRecipeCount)
                {
                    return false; // Setup failed
                }

                // Act: Perform deletion
                bool deletionSucceeded = false;
                try
                {
                    mealPlanService.DeleteAsync(testData.MealPlanId, testData.AccountId).Wait();
                    deletionSucceeded = true;
                }
                catch (Exception)
                {
                    // Deletion failed - this is acceptable for testing atomicity
                    deletionSucceeded = false;
                }

                // Assert: Verify transactional atomicity
                var mealPlanAfterDelete = context.MealPlans.Find(testData.MealPlanId);
                var mealsAfterDelete = context.Meals.Where(m => m.PlanId == testData.MealPlanId).ToList();
                var mealRecipesAfterDelete = context.MealRecipes
                    .Where(mr => mealIds.Contains(mr.MealId))
                    .ToList();
                var recipesAfterDelete = context.Recipes.Where(r => recipeIds.Contains(r.Id)).ToList();

                if (deletionSucceeded)
                {
                    // Property 1: If deletion succeeded, ALL related records must be deleted
                    bool mealPlanDeleted = mealPlanAfterDelete == null;
                    bool allMealsDeleted = mealsAfterDelete.Count == 0;
                    bool allMealRecipesDeleted = mealRecipesAfterDelete.Count == 0;
                    bool recipesNotDeleted = recipesAfterDelete.Count == recipeIds.Count;

                    // Atomicity: All deletions must succeed together
                    return mealPlanDeleted 
                        && allMealsDeleted 
                        && allMealRecipesDeleted 
                        && recipesNotDeleted;
                }
                else
                {
                    // Property 2: If deletion failed, NO records should be deleted (rollback)
                    bool mealPlanStillExists = mealPlanAfterDelete != null;
                    bool allMealsStillExist = mealsAfterDelete.Count == testData.MealCount;
                    bool allMealRecipesStillExist = mealRecipesAfterDelete.Count == mealRecipeCount;
                    bool recipesStillExist = recipesAfterDelete.Count == recipeIds.Count;

                    // Atomicity: All records must remain if deletion failed
                    return mealPlanStillExists 
                        && allMealsStillExist 
                        && allMealRecipesStillExist 
                        && recipesStillExist;
                }
            });
    }
}
