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
/// Property-based tests for RecipeService and IngredientService
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class RecipeServicePropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IRecipeService _recipeService;
    private IIngredientService _ingredientService;
    private Mock<ILogger<RecipeService>> _mockRecipeLogger;
    private Mock<ILogger<IngredientService>> _mockIngredientLogger;

    public RecipeServicePropertyTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockRecipeLogger = new Mock<ILogger<RecipeService>>();
        _mockIngredientLogger = new Mock<ILogger<IngredientService>>();
        _recipeService = new RecipeService(_unitOfWork, _mockRecipeLogger.Object);
        _ingredientService = new IngredientService(_unitOfWork, _mockIngredientLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _unitOfWork.Dispose();
    }

    /// <summary>
    /// Property 51: Recipe required fields
    /// For any recipe creation, recipe name and instructions must be provided
    /// Validates: Requirements 11.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RecipeRequiredFields()
    {
        return Prop.ForAll(
            GenerateCreateRecipeDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<RecipeService>>();
                var recipeService = new RecipeService(unitOfWork, mockLogger.Object);

                // Act & Assert
                if (string.IsNullOrWhiteSpace(dto.RecipeName) || string.IsNullOrWhiteSpace(dto.Instructions))
                {
                    // Should throw BusinessException for missing required fields
                    try
                    {
                        recipeService.CreateRecipeAsync(dto).Wait();
                        return false; // Should not reach here
                    }
                    catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                    {
                        return ex.Message.Contains("required");
                    }
                    catch
                    {
                        return false; // Wrong exception type
                    }
                }
                else
                {
                    // Should succeed with valid fields
                    var result = recipeService.CreateRecipeAsync(dto).Result;
                    return result != null
                        && result.RecipeName == dto.RecipeName.Trim()
                        && result.Instructions == dto.Instructions.Trim();
                }
            });
    }

    /// <summary>
    /// Property 52: Recipe ingredient required fields
    /// For any recipe ingredient addition, ingredient ID and positive amount must be provided
    /// Validates: Requirements 11.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RecipeIngredientRequiredFields()
    {
        return Prop.ForAll(
            GenerateValidCreateRecipeDto(),
            GenerateRecipeIngredientDto(),
            (recipeDto, ingredientDto) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockRecipeLogger = new Mock<ILogger<RecipeService>>();
                var mockIngredientLogger = new Mock<ILogger<IngredientService>>();
                var recipeService = new RecipeService(unitOfWork, mockRecipeLogger.Object);
                var ingredientService = new IngredientService(unitOfWork, mockIngredientLogger.Object);

                // Create a recipe first
                var recipe = recipeService.CreateRecipeAsync(recipeDto).Result;

                // Create an ingredient if needed
                Guid ingredientId = ingredientDto.IngredientId;
                if (ingredientDto.IngredientId != Guid.Empty)
                {
                    var createIngredientDto = new CreateIngredientDto
                    {
                        IngredientName = "Test Ingredient",
                        Unit = "g",
                        CaloPerUnit = 100,
                        IsAllergen = false
                    };
                    var ingredient = ingredientService.CreateIngredientAsync(createIngredientDto).Result;
                    ingredientId = ingredient.Id;
                    ingredientDto.IngredientId = ingredientId;
                }

                // Act & Assert
                if (ingredientDto.IngredientId == Guid.Empty || ingredientDto.Amount <= 0)
                {
                    // Should throw BusinessException for invalid fields
                    try
                    {
                        recipeService.AddIngredientToRecipeAsync(recipe.Id, ingredientDto).Wait();
                        return false; // Should not reach here
                    }
                    catch (AggregateException ae) when (ae.InnerException is BusinessException)
                    {
                        return true;
                    }
                    catch
                    {
                        return false; // Wrong exception type
                    }
                }
                else
                {
                    // Should succeed with valid fields
                    try
                    {
                        recipeService.AddIngredientToRecipeAsync(recipe.Id, ingredientDto).Wait();
                        return true;
                    }
                    catch
                    {
                        return false; // Should not throw
                    }
                }
            });
    }

    /// <summary>
    /// Property 53: Ingredient required fields
    /// For any ingredient creation, ingredient name, unit, and non-negative calories must be provided
    /// Validates: Requirements 11.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property IngredientRequiredFields()
    {
        return Prop.ForAll(
            GenerateCreateIngredientDto(),
            dto =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<IngredientService>>();
                var ingredientService = new IngredientService(unitOfWork, mockLogger.Object);

                // Act & Assert
                if (string.IsNullOrWhiteSpace(dto.IngredientName) || 
                    string.IsNullOrWhiteSpace(dto.Unit) || 
                    dto.CaloPerUnit < 0)
                {
                    // Should throw BusinessException for missing/invalid required fields
                    try
                    {
                        ingredientService.CreateIngredientAsync(dto).Wait();
                        return false; // Should not reach here
                    }
                    catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                    {
                        return ex.Message.Contains("required") || ex.Message.Contains("non-negative");
                    }
                    catch
                    {
                        return false; // Wrong exception type
                    }
                }
                else
                {
                    // Should succeed with valid fields
                    var result = ingredientService.CreateIngredientAsync(dto).Result;
                    return result != null
                        && result.IngredientName == dto.IngredientName.Trim()
                        && result.Unit == dto.Unit.Trim()
                        && result.CaloPerUnit == dto.CaloPerUnit
                        && result.IsAllergen == dto.IsAllergen;
                }
            });
    }

    /// <summary>
    /// Property 55: Recipe deletion constraint
    /// For any recipe used in active menu meals, deletion should be prevented
    /// Validates: Requirements 11.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RecipeDeletionConstraint()
    {
        return Prop.ForAll(
            GenerateValidCreateRecipeDto(),
            Arb.From(Gen.Elements(true, false)),
            (recipeDto, isUsedInActiveMenu) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<RecipeService>>();
                var recipeService = new RecipeService(unitOfWork, mockLogger.Object);

                // Create a recipe
                var recipe = recipeService.CreateRecipeAsync(recipeDto).Result;

                // If testing active menu constraint, create an active menu with this recipe
                if (isUsedInActiveMenu)
                {
                    var dailyMenu = new DailyMenu
                    {
                        Id = Guid.NewGuid(),
                        MenuDate = DateTime.UtcNow.Date,
                        Status = "active",
                        CreatedAt = DateTime.UtcNow
                    };
                    context.DailyMenus.Add(dailyMenu);
                    context.SaveChanges();

                    var menuMeal = new MenuMeal
                    {
                        Id = Guid.NewGuid(),
                        MenuId = dailyMenu.Id,
                        RecipeId = recipe.Id,
                        Price = 10.00m,
                        AvailableQuantity = 10,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.MenuMeals.Add(menuMeal);
                    context.SaveChanges();
                }

                // Act & Assert
                if (isUsedInActiveMenu)
                {
                    // Should throw BusinessException when recipe is used in active menu
                    try
                    {
                        recipeService.DeleteRecipeAsync(recipe.Id).Wait();
                        return false; // Should not reach here
                    }
                    catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                    {
                        return ex.Message.Contains("Cannot delete recipe") && 
                               ex.Message.Contains("active menu");
                    }
                    catch
                    {
                        return false; // Wrong exception type
                    }
                }
                else
                {
                    // Should succeed when recipe is not used in active menu
                    try
                    {
                        recipeService.DeleteRecipeAsync(recipe.Id).Wait();
                        
                        // Verify recipe is deleted
                        var deletedRecipe = unitOfWork.Recipes.GetByIdAsync(recipe.Id).Result;
                        return deletedRecipe == null;
                    }
                    catch
                    {
                        return false; // Should not throw
                    }
                }
            });
    }

    #region Generators

    /// <summary>
    /// Generator for CreateRecipeDto (may have empty fields for testing validation)
    /// </summary>
    private static Arbitrary<CreateRecipeDto> GenerateCreateRecipeDto()
    {
        var gen = from recipeName in Gen.OneOf(
                      GenerateNonEmptyString(3, 50),
                      Gen.Constant(""),
                      Gen.Constant("   "))
                  from instructions in Gen.OneOf(
                      GenerateNonEmptyString(10, 200),
                      Gen.Constant(""),
                      Gen.Constant("   "))
                  select new CreateRecipeDto
                  {
                      RecipeName = recipeName,
                      Instructions = instructions
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for valid CreateRecipeDto (always has required fields)
    /// </summary>
    private static Arbitrary<CreateRecipeDto> GenerateValidCreateRecipeDto()
    {
        var gen = from recipeName in GenerateNonEmptyString(3, 50)
                  from instructions in GenerateNonEmptyString(10, 200)
                  select new CreateRecipeDto
                  {
                      RecipeName = recipeName,
                      Instructions = instructions
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for CreateIngredientDto (may have empty/invalid fields for testing validation)
    /// </summary>
    private static Arbitrary<CreateIngredientDto> GenerateCreateIngredientDto()
    {
        var gen = from ingredientName in Gen.OneOf(
                      GenerateNonEmptyString(3, 50),
                      Gen.Constant(""),
                      Gen.Constant("   "))
                  from unit in Gen.OneOf(
                      GenerateNonEmptyString(1, 10),
                      Gen.Constant(""),
                      Gen.Constant("   "))
                  from caloPerUnit in Gen.Choose(-100, 1000).Select(x => (float)x)
                  from isAllergen in Arb.Generate<bool>()
                  select new CreateIngredientDto
                  {
                      IngredientName = ingredientName,
                      Unit = unit,
                      CaloPerUnit = caloPerUnit,
                      IsAllergen = isAllergen
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for RecipeIngredientDto (may have invalid fields for testing validation)
    /// </summary>
    private static Arbitrary<RecipeIngredientDto> GenerateRecipeIngredientDto()
    {
        var gen = from hasIngredientId in Arb.Generate<bool>()
                  from amount in Gen.Choose(-10, 1000).Select(x => (float)x)
                  select new RecipeIngredientDto
                  {
                      IngredientId = hasIngredientId ? Guid.NewGuid() : Guid.Empty,
                      Amount = amount
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

    #endregion
}
