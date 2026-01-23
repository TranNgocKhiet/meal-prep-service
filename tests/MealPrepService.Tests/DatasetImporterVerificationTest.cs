using Microsoft.EntityFrameworkCore;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.Web.Data;
using OfficeOpenXml;

namespace MealPrepService.Tests;

/// <summary>
/// Verification test to check actual data values from import
/// </summary>
public class DatasetImporterVerificationTest : IDisposable
{
    private readonly MealPrepDbContext _context;
    private readonly string _testFilesDirectory;
    private readonly DatasetImporter _importer;

    public DatasetImporterVerificationTest()
    {
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _testFilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "files");
        _importer = new DatasetImporter(_context, _testFilesDirectory);
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "3.1")]
    public async Task ImportIngredientsAsync_VerifyActualData()
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

        // Assert: Check specific ingredient data
        var ingredients = await _context.Ingredients.ToListAsync();
        
        // Find "Acai Berries" which should be the first ingredient
        var acaiBerries = ingredients.FirstOrDefault(i => i.IngredientName == "Acai Berries");
        Assert.NotNull(acaiBerries);
        Assert.Equal("g", acaiBerries.Unit);
        Assert.True(acaiBerries.CaloPerUnit > 0, $"CaloPerUnit should be positive, got {acaiBerries.CaloPerUnit}");
        
        // Print first 5 ingredients for verification
        Console.WriteLine("\nFirst 5 imported ingredients:");
        foreach (var ingredient in ingredients.Take(5))
        {
            Console.WriteLine($"- {ingredient.IngredientName}: {ingredient.CaloPerUnit} {ingredient.Unit}, IsAllergen: {ingredient.IsAllergen}");
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
