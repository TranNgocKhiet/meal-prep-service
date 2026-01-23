using Microsoft.EntityFrameworkCore;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.Web.Data;
using OfficeOpenXml;

namespace MealPrepService.Tests;

/// <summary>
/// Unit tests for ImportRecipeIngredientsAsync method
/// Tests task 5.1 requirements
/// </summary>
public class ImportRecipeIngredientsTests : IDisposable
{
    private readonly MealPrepDbContext _context;
    private readonly string _testFilesDirectory;
    private readonly DatasetImporter _importer;

    public ImportRecipeIngredientsTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
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
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_OpensCorrectFile()
    {
        // Arrange: Verify file exists
        var filePath = Path.Combine(_testFilesDirectory, "Dataset_Recipe_Ingredient.xlsx");
        Assert.True(File.Exists(filePath), "Dataset_Recipe_Ingredient.xlsx should exist");

        // Act: Import recipe ingredients using reflection
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Should complete without throwing
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        Assert.NotEmpty(recipeIngredients);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_ReadsFirstWorksheet()
    {
        // Act: Import recipe ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Data should be imported from first worksheet
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        Assert.NotEmpty(recipeIngredients);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_StartsFromRow2()
    {
        // Act: Import recipe ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Should have imported data (header row skipped)
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        Assert.NotEmpty(recipeIngredients);
        
        // Verify first record has valid GUIDs (not header text)
        var firstRecord = recipeIngredients.First();
        Assert.NotEqual(Guid.Empty, firstRecord.RecipeId);
        Assert.NotEqual(Guid.Empty, firstRecord.IngredientId);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_ReadsCorrectColumns()
    {
        // Act: Import recipe ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Verify columns are read correctly
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        Assert.NotEmpty(recipeIngredients);
        
        // Verify all records have valid data from columns A, B, C
        Assert.All(recipeIngredients, ri =>
        {
            Assert.NotEqual(Guid.Empty, ri.RecipeId); // Column A
            Assert.NotEqual(Guid.Empty, ri.IngredientId); // Column B
            Assert.True(ri.Amount > 0, "Amount should be positive"); // Column C
        });
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_CreatesRecipeIngredientEntities()
    {
        // Arrange: Database is empty
        var initialCount = await _context.RecipeIngredients.CountAsync();
        Assert.Equal(0, initialCount);

        // Act: Import recipe ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: RecipeIngredient entities should be created
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        Assert.NotEmpty(recipeIngredients);
        Assert.IsType<RecipeIngredient>(recipeIngredients.First());
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_AddsEntitiesToDbContext()
    {
        // Act: Import recipe ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Entities should be tracked by DbContext
        var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
        Assert.NotEmpty(recipeIngredients);
        
        // Verify entities are in the context
        Assert.All(recipeIngredients, ri =>
        {
            var entry = _context.Entry(ri);
            Assert.NotNull(entry);
        });
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_SavesChanges()
    {
        // Arrange: Database is empty
        var initialCount = await _context.RecipeIngredients.CountAsync();
        Assert.Equal(0, initialCount);

        // Act: Import recipe ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Changes should be saved to database
        var finalCount = await _context.RecipeIngredients.CountAsync();
        Assert.True(finalCount > 0, "Database should contain saved recipe-ingredient relationships");
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task ImportRecipeIngredientsAsync_OutputsCountToConsole()
    {
        // Arrange: Capture console output
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act: Import recipe ingredients
            var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);
            
            var task = (Task?)method.Invoke(_importer, null);
            if (task != null)
            {
                await task;
            }

            // Assert: Console output should contain count
            var output = stringWriter.ToString();
            Assert.Contains("Imported", output);
            Assert.Contains("recipe-ingredient", output);
            
            // Verify count is a number
            var count = await _context.RecipeIngredients.CountAsync();
            Assert.Contains(count.ToString(), output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
