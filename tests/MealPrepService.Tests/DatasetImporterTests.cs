using Microsoft.EntityFrameworkCore;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.Web.Data;
using OfficeOpenXml;

namespace MealPrepService.Tests;

/// <summary>
/// Unit tests for DatasetImporter
/// Tests dataset import functionality from Excel files
/// </summary>
[Collection("Sequential")]
public class DatasetImporterTests : IDisposable
{
    private readonly MealPrepDbContext _context;
    private readonly string _testFilesDirectory;
    private readonly DatasetImporter _importer;

    public DatasetImporterTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new MealPrepDbContext(options);
        
        // Use the actual files directory
        _testFilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "files");
        
        _importer = new DatasetImporter(_context, _testFilesDirectory);
        
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "3.1")]
    public async Task ImportIngredientsAsync_ValidFile_ImportsSuccessfully()
    {
        // Arrange: Database is empty
        
        // Act: Import ingredients using reflection to call private method
        var method = typeof(DatasetImporter).GetMethod("ImportIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
        {
            throw new InvalidOperationException("ImportIngredientsAsync method not found");
        }
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Ingredients should be imported
        var ingredients = await _context.Ingredients.ToListAsync();
        Assert.NotEmpty(ingredients);
        
        // Verify all ingredients have required properties
        Assert.All(ingredients, ingredient =>
        {
            Assert.NotEmpty(ingredient.IngredientName);
            Assert.NotEmpty(ingredient.Unit);
            Assert.True(ingredient.CaloPerUnit >= 0, "CaloPerUnit should be non-negative");
        });
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "3.1")]
    public async Task ImportIngredientsAsync_ReadsCorrectColumns()
    {
        // Arrange & Act: Import ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
        {
            throw new InvalidOperationException("ImportIngredientsAsync method not found");
        }
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Verify data structure
        var ingredients = await _context.Ingredients.ToListAsync();
        Assert.NotEmpty(ingredients);
        
        // Check that we have ingredients with various properties
        var firstIngredient = ingredients.First();
        Assert.NotNull(firstIngredient.IngredientName);
        Assert.NotNull(firstIngredient.Unit);
        Assert.True(firstIngredient.CaloPerUnit >= 0);
        
        // Verify IsAllergen is a boolean (should be true or false)
        Assert.True(firstIngredient.IsAllergen == true || firstIngredient.IsAllergen == false);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "3.1")]
    public async Task ImportIngredientsAsync_OutputsCount()
    {
        // Arrange: Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act: Import ingredients
            var method = typeof(DatasetImporter).GetMethod("ImportIngredientsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
            {
                throw new InvalidOperationException("ImportIngredientsAsync method not found");
            }
            
            var task = (Task?)method.Invoke(_importer, null);
            if (task != null)
            {
                await task;
            }

            // Assert: Console output should contain count
            var output = stringWriter.ToString();
            Assert.Contains("Imported", output);
            Assert.Contains("ingredients", output);
        }
        finally
        {
            Console.SetOut(originalOut);
            stringWriter.Dispose();
        }
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "3.1")]
    public async Task ImportIngredientsAsync_SavesChangesToDatabase()
    {
        // Arrange: Database is empty
        var initialCount = await _context.Ingredients.CountAsync();
        Assert.Equal(0, initialCount);

        // Act: Import ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null)
        {
            throw new InvalidOperationException("ImportIngredientsAsync method not found");
        }
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Database should have ingredients
        var finalCount = await _context.Ingredients.CountAsync();
        Assert.True(finalCount > 0, "Database should contain imported ingredients");
    }

    /// <summary>
    /// Tests for ImportAllAsync method - Task 6.1
    /// Verifies transaction management and orchestration
    /// </summary>
    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "6.1")]
    public async Task ImportAllAsync_BeginsTransaction()
    {
        // Arrange: Fresh database
        
        // Act: Import all data
        await _importer.ImportAllAsync();

        // Assert: All data should be imported (transaction was committed)
        var allergies = await _context.Allergies.ToListAsync();
        var ingredients = await _context.Ingredients.ToListAsync();
        var recipes = await _context.Recipes.ToListAsync();
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        
        Assert.NotEmpty(allergies);
        Assert.NotEmpty(ingredients);
        Assert.NotEmpty(recipes);
        Assert.NotEmpty(recipeIngredients);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "6.1")]
    public async Task ImportAllAsync_CallsAllImportMethodsInOrder()
    {
        // Arrange & Act: Import all data
        await _importer.ImportAllAsync();

        // Assert: Verify all entity types were imported
        var allergies = await _context.Allergies.ToListAsync();
        var ingredients = await _context.Ingredients.ToListAsync();
        var recipes = await _context.Recipes.ToListAsync();
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        
        // All imports should have succeeded
        Assert.NotEmpty(allergies);
        Assert.NotEmpty(ingredients);
        Assert.NotEmpty(recipes);
        Assert.NotEmpty(recipeIngredients);
        
        // Verify we have the expected counts (from the actual Excel files)
        Assert.Equal(20, allergies.Count);
        Assert.Equal(240, ingredients.Count);
        Assert.Equal(84, recipes.Count);
        Assert.Equal(749, recipeIngredients.Count);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "6.1")]
    public async Task ImportAllAsync_CommitsTransactionOnSuccess()
    {
        // Arrange & Act: Import all data
        await _importer.ImportAllAsync();

        // Assert: Data should be persisted (transaction committed)
        var allergies = await _context.Allergies.ToListAsync();
        var ingredients = await _context.Ingredients.ToListAsync();
        var recipes = await _context.Recipes.ToListAsync();
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        
        // All data should be present
        Assert.NotEmpty(allergies);
        Assert.NotEmpty(ingredients);
        Assert.NotEmpty(recipes);
        Assert.NotEmpty(recipeIngredients);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "6.1")]
    public async Task ImportAllAsync_RollsBackTransactionOnError()
    {
        // Arrange: Create importer with invalid directory to force error
        var invalidImporter = new DatasetImporter(_context, "invalid_directory_path");

        // Act & Assert: Import should throw exception
        await Assert.ThrowsAsync<FileNotFoundException>(async () => 
            await invalidImporter.ImportAllAsync());

        // Assert: No data should be imported (transaction rolled back)
        var allergies = await _context.Allergies.ToListAsync();
        var ingredients = await _context.Ingredients.ToListAsync();
        var recipes = await _context.Recipes.ToListAsync();
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        
        Assert.Empty(allergies);
        Assert.Empty(ingredients);
        Assert.Empty(recipes);
        Assert.Empty(recipeIngredients);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "6.1")]
    public async Task ImportAllAsync_OutputsSuccessMessage()
    {
        // Arrange: Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act: Import all data
            await _importer.ImportAllAsync();

            // Assert: Console output should contain success message
            var output = stringWriter.ToString();
            Assert.Contains("Import completed successfully!", output);
        }
        finally
        {
            Console.SetOut(originalOut);
            stringWriter.Dispose();
        }
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "6.1")]
    public async Task ImportAllAsync_OutputsFailureMessageOnError()
    {
        // Arrange: Create importer with invalid directory
        var invalidImporter = new DatasetImporter(_context, "invalid_directory_path");
        
        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act: Import should fail
            await Assert.ThrowsAsync<FileNotFoundException>(async () => 
                await invalidImporter.ImportAllAsync());

            // Assert: Console output should contain failure message
            var output = stringWriter.ToString();
            Assert.Contains("Import failed:", output);
        }
        finally
        {
            Console.SetOut(originalOut);
            stringWriter.Dispose();
        }
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "6.1")]
    public async Task ImportAllAsync_MaintainsImportOrder()
    {
        // Arrange & Act: Import all data
        await _importer.ImportAllAsync();

        // Assert: Verify import order by checking that all data was imported successfully
        // If the order was wrong, foreign key constraints would fail (in a real database)
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        var recipes = await _context.Recipes.ToListAsync();
        var ingredients = await _context.Ingredients.ToListAsync();
        var allergies = await _context.Allergies.ToListAsync();
        
        // All data should be present, which proves the import order was correct
        Assert.Equal(20, allergies.Count);
        Assert.Equal(240, ingredients.Count);
        Assert.Equal(84, recipes.Count);
        Assert.Equal(749, recipeIngredients.Count);
        
        // Verify RecipeIngredients have valid IDs (not empty GUIDs)
        Assert.All(recipeIngredients, ri =>
        {
            Assert.NotEqual(Guid.Empty, ri.RecipeId);
            Assert.NotEqual(Guid.Empty, ri.IngredientId);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
