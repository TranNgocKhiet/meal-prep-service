using Microsoft.EntityFrameworkCore;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.Web.Data;
using OfficeOpenXml;

namespace MealPrepService.Tests;

/// <summary>
/// Verification test to ensure column mappings are correct for ImportRecipeIngredientsAsync
/// </summary>
public class VerifyRecipeIngredientColumnMapping : IDisposable
{
    private readonly MealPrepDbContext _context;
    private readonly string _testFilesDirectory;
    private readonly DatasetImporter _importer;

    public VerifyRecipeIngredientColumnMapping()
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
    [Trait("Task", "5.1")]
    public async Task VerifyColumnMapping_RecipeId_IngredientId_Amount()
    {
        // Arrange: Read first data row from Excel
        var filePath = Path.Combine(_testFilesDirectory, "Dataset_Recipe_Ingredient.xlsx");
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets[0];
        
        var expectedRecipeId = Guid.Parse(worksheet.Cells[2, 1].Value?.ToString() ?? throw new Exception("RecipeId not found"));
        var expectedIngredientId = Guid.Parse(worksheet.Cells[2, 2].Value?.ToString() ?? throw new Exception("IngredientId not found"));
        var expectedAmount = float.Parse(worksheet.Cells[2, 3].Value?.ToString() ?? throw new Exception("Amount not found"));

        // Act: Import recipe ingredients
        var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(method);
        
        var task = (Task?)method.Invoke(_importer, null);
        if (task != null)
        {
            await task;
        }

        // Assert: Verify first imported record matches Excel data
        var firstRecord = await _context.RecipeIngredients.FirstAsync();
        
        Assert.Equal(expectedRecipeId, firstRecord.RecipeId);
        Assert.Equal(expectedIngredientId, firstRecord.IngredientId);
        Assert.Equal(expectedAmount, firstRecord.Amount);
    }

    [Fact]
    [Trait("Feature", "dataset-import-service")]
    [Trait("Task", "5.1")]
    public async Task VerifyAllRequirements_Task5_1()
    {
        // This test verifies all requirements from task 5.1 in one comprehensive test
        
        // Arrange: Capture console output
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Requirement: Opens Dataset_Recipe_Ingredient.xlsx using EPPlus
            var filePath = Path.Combine(_testFilesDirectory, "Dataset_Recipe_Ingredient.xlsx");
            Assert.True(File.Exists(filePath), "File should exist");

            // Act: Import recipe ingredients
            var method = typeof(DatasetImporter).GetMethod("ImportRecipeIngredientsAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);
            
            var task = (Task?)method.Invoke(_importer, null);
            if (task != null)
            {
                await task;
            }

            // Requirement: Reads first worksheet
            // Requirement: Loops through rows starting from row 2
            // Requirement: Reads RecipeId (column A), IngredientId (column B), Amount (column C)
            var recipeIngredients = await _context.RecipeIngredients.ToListAsync();
            Assert.NotEmpty(recipeIngredients);
            
            // Verify data structure
            var firstRecord = recipeIngredients.First();
            Assert.NotEqual(Guid.Empty, firstRecord.RecipeId); // Column A
            Assert.NotEqual(Guid.Empty, firstRecord.IngredientId); // Column B
            Assert.True(firstRecord.Amount > 0); // Column C

            // Requirement: Creates RecipeIngredient entities
            Assert.All(recipeIngredients, ri => Assert.IsType<DataAccessLayer.Entities.RecipeIngredient>(ri));

            // Requirement: Adds entities to DbContext
            // Requirement: Saves changes
            var count = await _context.RecipeIngredients.CountAsync();
            Assert.True(count > 0, "Entities should be saved to database");

            // Requirement: Outputs count to console
            var output = stringWriter.ToString();
            Assert.Contains("Imported", output);
            Assert.Contains(count.ToString(), output);
            Assert.Contains("recipe-ingredient", output);
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
