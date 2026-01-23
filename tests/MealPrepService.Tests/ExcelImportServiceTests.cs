using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.BusinessLogicLayer.Validators;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using OfficeOpenXml;

namespace MealPrepService.Tests;

/// <summary>
/// Unit tests for ExcelImportService
/// Tests Excel import functionality, validation, and error handling
/// </summary>
public class ExcelImportServiceTests : IDisposable
{
    private readonly MealPrepDbContext _context;
    private readonly IValidator<TrainingDataset> _validator;
    private readonly ILogger<ExcelImportService> _logger;
    private readonly ExcelImportService _service;
    private readonly string _testFilePath;

    public ExcelImportServiceTests()
    {
        // Set up in-memory database
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new MealPrepDbContext(options);
        _validator = new TrainingDatasetValidator();
        _logger = LoggerFactory.Create(builder => { })
            .CreateLogger<ExcelImportService>();
        _service = new ExcelImportService(_context, _validator, _logger);
        
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_import_{Guid.NewGuid()}.xlsx");
        
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    [Fact]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public async Task ImportFromExcelAsync_ValidFile_ImportsSuccessfully()
    {
        // Arrange: Create a valid Excel file
        CreateValidTestExcelFile(_testFilePath);

        // Act: Import from Excel
        var result = await _service.ImportFromExcelAsync(_testFilePath);

        // Assert: Import should succeed
        Assert.True(result.SuccessCount > 0, "Should import at least one record");
        Assert.Equal(0, result.ErrorCount);
        Assert.Empty(result.Errors);
        
        // Verify data in database
        var datasets = await _context.TrainingDatasets.ToListAsync();
        Assert.NotEmpty(datasets);
        Assert.All(datasets, dataset =>
        {
            Assert.NotEmpty(dataset.CustomerSegment);
            Assert.NotEmpty(dataset.PreferredMealTypes);
            Assert.True(dataset.AverageCalorieTarget > 0);
            Assert.NotEmpty(dataset.RecommendationWeights);
        });
    }

    [Fact]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public async Task ImportFromExcelAsync_MixedValidInvalidRows_ImportsValidRowsAndLogsErrors()
    {
        // Arrange: Create Excel file with mixed valid and invalid rows
        CreateMixedValidityTestExcelFile(_testFilePath);

        // Act: Import from Excel
        var result = await _service.ImportFromExcelAsync(_testFilePath);

        // Assert: Should import valid rows and log errors for invalid ones
        Assert.True(result.SuccessCount > 0, "Should import at least one valid record");
        Assert.True(result.ErrorCount > 0, "Should have at least one error");
        Assert.NotEmpty(result.Errors);
        
        // Verify only valid data in database
        var datasets = await _context.TrainingDatasets.ToListAsync();
        Assert.NotEmpty(datasets);
        Assert.All(datasets, dataset =>
        {
            var validationResult = _validator.Validate(dataset);
            Assert.True(validationResult.IsValid, "All imported datasets should be valid");
        });
    }

    [Fact]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public async Task ImportFromExcelAsync_FileNotFound_ReturnsErrorResult()
    {
        // Arrange: Use non-existent file path
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_file.xlsx");

        // Act: Import from Excel
        var result = await _service.ImportFromExcelAsync(nonExistentPath);

        // Assert: Should return error result
        Assert.Equal(0, result.SuccessCount);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("not found", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public async Task ImportFromExcelAsync_EmptyFile_ReturnsErrorResult()
    {
        // Arrange: Create empty Excel file
        CreateEmptyTestExcelFile(_testFilePath);

        // Act: Import from Excel
        var result = await _service.ImportFromExcelAsync(_testFilePath);

        // Assert: Should return error result
        Assert.Equal(0, result.SuccessCount);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public async Task ShouldAutoImportAsync_EmptyDatabase_ReturnsTrue()
    {
        // Arrange: Database is empty (no setup needed)

        // Act: Check if should auto-import
        var shouldImport = await _service.ShouldAutoImportAsync();

        // Assert: Should return true for empty database
        Assert.True(shouldImport);
    }

    [Fact]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public async Task ShouldAutoImportAsync_DatabaseHasData_ReturnsFalse()
    {
        // Arrange: Add a dataset to the database
        var dataset = new TrainingDataset
        {
            CustomerSegment = "Test",
            PreferredMealTypes = "[\"Breakfast\"]",
            AverageCalorieTarget = 2000,
            RecommendationWeights = "{\"test\":1}",
            CreatedAt = DateTime.UtcNow
        };
        await _context.TrainingDatasets.AddAsync(dataset);
        await _context.SaveChangesAsync();

        // Act: Check if should auto-import
        var shouldImport = await _service.ShouldAutoImportAsync();

        // Assert: Should return false when database has data
        Assert.False(shouldImport);
    }

    [Fact]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public async Task ImportFromExcelAsync_CommaSeparatedValues_ConvertsToJsonArrays()
    {
        // Arrange: Create Excel file with comma-separated values
        CreateCommaSeparatedTestExcelFile(_testFilePath);

        // Act: Import from Excel
        var result = await _service.ImportFromExcelAsync(_testFilePath);

        // Assert: Should convert comma-separated values to JSON arrays
        Assert.True(result.SuccessCount > 0);
        
        var datasets = await _context.TrainingDatasets.ToListAsync();
        Assert.NotEmpty(datasets);
        Assert.All(datasets, dataset =>
        {
            Assert.StartsWith("[", dataset.PreferredMealTypes);
            Assert.EndsWith("]", dataset.PreferredMealTypes);
        });
    }

    #region Helper Methods

    private void CreateValidTestExcelFile(string filePath)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("TrainingData");

        // Add headers
        worksheet.Cells[1, 1].Value = "CustomerSegment";
        worksheet.Cells[1, 2].Value = "PreferredMealTypes";
        worksheet.Cells[1, 3].Value = "AverageCalorieTarget";
        worksheet.Cells[1, 4].Value = "CommonAllergies";
        worksheet.Cells[1, 5].Value = "RecommendationWeights";

        // Add valid data rows
        worksheet.Cells[2, 1].Value = "Athletic";
        worksheet.Cells[2, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\"]";
        worksheet.Cells[2, 3].Value = 2500;
        worksheet.Cells[2, 4].Value = "[\"Peanuts\"]";
        worksheet.Cells[2, 5].Value = "{\"protein\":0.4,\"carbs\":0.3,\"fats\":0.3}";

        worksheet.Cells[3, 1].Value = "Weight Loss";
        worksheet.Cells[3, 2].Value = "[\"Breakfast\",\"Lunch\"]";
        worksheet.Cells[3, 3].Value = 1800;
        worksheet.Cells[3, 4].Value = "[]";
        worksheet.Cells[3, 5].Value = "{\"protein\":0.35,\"carbs\":0.4,\"fats\":0.25}";

        package.SaveAs(new FileInfo(filePath));
    }

    private void CreateMixedValidityTestExcelFile(string filePath)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("TrainingData");

        // Add headers
        worksheet.Cells[1, 1].Value = "CustomerSegment";
        worksheet.Cells[1, 2].Value = "PreferredMealTypes";
        worksheet.Cells[1, 3].Value = "AverageCalorieTarget";
        worksheet.Cells[1, 4].Value = "CommonAllergies";
        worksheet.Cells[1, 5].Value = "RecommendationWeights";

        // Valid row
        worksheet.Cells[2, 1].Value = "Athletic";
        worksheet.Cells[2, 2].Value = "[\"Breakfast\",\"Lunch\"]";
        worksheet.Cells[2, 3].Value = 2500;
        worksheet.Cells[2, 4].Value = "[]";
        worksheet.Cells[2, 5].Value = "{\"protein\":0.4}";

        // Invalid row - negative calories
        worksheet.Cells[3, 1].Value = "Invalid";
        worksheet.Cells[3, 2].Value = "[\"Breakfast\"]";
        worksheet.Cells[3, 3].Value = -100;
        worksheet.Cells[3, 4].Value = "[]";
        worksheet.Cells[3, 5].Value = "{\"test\":1}";

        // Invalid row - missing required field
        worksheet.Cells[4, 1].Value = "";
        worksheet.Cells[4, 2].Value = "[\"Lunch\"]";
        worksheet.Cells[4, 3].Value = 2000;
        worksheet.Cells[4, 4].Value = "[]";
        worksheet.Cells[4, 5].Value = "{\"test\":1}";

        // Valid row
        worksheet.Cells[5, 1].Value = "Diabetic";
        worksheet.Cells[5, 2].Value = "[\"Breakfast\",\"Dinner\"]";
        worksheet.Cells[5, 3].Value = 2000;
        worksheet.Cells[5, 4].Value = "[\"Sugar\"]";
        worksheet.Cells[5, 5].Value = "{\"protein\":0.3,\"carbs\":0.5}";

        package.SaveAs(new FileInfo(filePath));
    }

    private void CreateEmptyTestExcelFile(string filePath)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("TrainingData");

        // Add only headers, no data rows
        worksheet.Cells[1, 1].Value = "CustomerSegment";
        worksheet.Cells[1, 2].Value = "PreferredMealTypes";
        worksheet.Cells[1, 3].Value = "AverageCalorieTarget";
        worksheet.Cells[1, 4].Value = "CommonAllergies";
        worksheet.Cells[1, 5].Value = "RecommendationWeights";

        package.SaveAs(new FileInfo(filePath));
    }

    private void CreateCommaSeparatedTestExcelFile(string filePath)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("TrainingData");

        // Add headers
        worksheet.Cells[1, 1].Value = "CustomerSegment";
        worksheet.Cells[1, 2].Value = "PreferredMealTypes";
        worksheet.Cells[1, 3].Value = "AverageCalorieTarget";
        worksheet.Cells[1, 4].Value = "CommonAllergies";
        worksheet.Cells[1, 5].Value = "RecommendationWeights";

        // Add data with comma-separated values (not JSON)
        worksheet.Cells[2, 1].Value = "Athletic";
        worksheet.Cells[2, 2].Value = "Breakfast, Lunch, Dinner";
        worksheet.Cells[2, 3].Value = 2500;
        worksheet.Cells[2, 4].Value = "Peanuts, Shellfish";
        worksheet.Cells[2, 5].Value = "{\"protein\":0.4,\"carbs\":0.3}";

        package.SaveAs(new FileInfo(filePath));
    }

    #endregion

    public void Dispose()
    {
        // Clean up test file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }

        _context.Dispose();
    }
}
