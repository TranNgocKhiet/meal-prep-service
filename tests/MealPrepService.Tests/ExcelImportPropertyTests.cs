using FsCheck;
using FsCheck.Xunit;
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
/// Property-based tests for Excel import validation
/// Tests universal properties that should hold for all Excel import scenarios
/// </summary>
public class ExcelImportPropertyTests : IDisposable
{
    private readonly List<string> _testFilesToCleanup = new();

    /// <summary>
    /// Property 5: Excel import row validation
    /// For any Excel file with mixed valid and invalid rows, the import process should validate each row independently
    /// and import all valid rows while logging errors for invalid ones.
    /// **Validates: Requirements 2.3, 2.4**
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Property", "Property 5: Excel import row validation")]
    public void ExcelImportRowValidation_MixedValidInvalidRows_ImportsValidAndLogsInvalid()
    {
        Prop.ForAll(
            GenerateMixedValidityExcelData(),
            (excelData) =>
            {
                try
                {
                    // Arrange: Set up in-memory database and service
                    var options = new DbContextOptionsBuilder<MealPrepDbContext>()
                        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                        .Options;

                    using var context = new MealPrepDbContext(options);
                    var validator = new TrainingDatasetValidator();
                    var logger = LoggerFactory.Create(builder => { })
                        .CreateLogger<ExcelImportService>();
                    var service = new ExcelImportService(context, validator, logger);

                    // Create Excel file with mixed data
                    var testFilePath = Path.Combine(Path.GetTempPath(), $"test_property_{Guid.NewGuid()}.xlsx");
                    _testFilesToCleanup.Add(testFilePath);
                    CreateExcelFile(testFilePath, excelData);

                    // Act: Import from Excel (synchronously for property test)
                    var result = service.ImportFromExcelAsync(testFilePath).GetAwaiter().GetResult();

                    // Assert: Verify each row was validated independently
                    // Count expected valid and invalid rows
                    var expectedValidCount = excelData.Rows.Count(r => r.IsValid);
                    var expectedInvalidCount = excelData.Rows.Count(r => !r.IsValid);

                    // Property 1: All valid rows should be imported
                    var actualValidCount = result.SuccessCount;
                    var validRowsImported = actualValidCount == expectedValidCount;

                    // Property 2: All invalid rows should be logged as errors
                    var actualInvalidCount = result.ErrorCount;
                    var invalidRowsLogged = actualInvalidCount == expectedInvalidCount;

                    // Property 3: Only valid data should be in database
                    var datasetsInDb = context.TrainingDatasets.ToList();
                    var onlyValidInDb = datasetsInDb.Count == expectedValidCount;

                    // Property 4: All imported datasets should pass validation
                    var allImportedAreValid = datasetsInDb.All(dataset =>
                    {
                        var validationResult = validator.Validate(dataset);
                        return validationResult.IsValid;
                    });

                    // Property 5: Error messages should be descriptive (non-empty)
                    var errorsAreDescriptive = result.ErrorCount == 0 || 
                        result.Errors.All(e => !string.IsNullOrWhiteSpace(e));

                    return validRowsImported
                        && invalidRowsLogged
                        && onlyValidInDb
                        && allImportedAreValid
                        && errorsAreDescriptive;
                }
                catch (Exception ex)
                {
                    // Log exception for debugging
                    Console.WriteLine($"Exception in property test: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            }).QuickCheckThrowOnFailure();
    }

    #region Generators

    /// <summary>
    /// Generate Excel data with mixed valid and invalid rows
    /// </summary>
    private static Arbitrary<ExcelTestData> GenerateMixedValidityExcelData()
    {
        var gen = from validCount in Gen.Choose(1, 5)
                  from invalidCount in Gen.Choose(1, 5)
                  from validRows in Gen.Sequence(Enumerable.Range(0, validCount).Select(_ => GenerateValidExcelRow()))
                  from invalidRows in Gen.Sequence(Enumerable.Range(0, invalidCount).Select(_ => GenerateInvalidExcelRow()))
                  let allRows = validRows.Concat(invalidRows).ToList()
                  from shuffledRows in Gen.Shuffle(allRows)
                  select new ExcelTestData
                  {
                      Rows = shuffledRows.ToList()
                  };

        return Arb.From(gen);
    }

    /// <summary>
    /// Generate a valid Excel row
    /// </summary>
    private static Gen<ExcelRowData> GenerateValidExcelRow()
    {
        return from segment in Gen.Elements("Athletic", "WeightLoss", "Diabetic", "Vegan", "Keto")
               from mealCount in Gen.Choose(1, 3)
               from mealTypes in Gen.Sequence(Enumerable.Range(0, mealCount)
                   .Select(_ => Gen.Elements("Breakfast", "Lunch", "Dinner", "Snack")))
               from calories in Gen.Choose(1000, 3000)
               from allergyCount in Gen.Choose(0, 2)
               from allergies in Gen.Sequence(Enumerable.Range(0, allergyCount)
                   .Select(_ => Gen.Elements("Peanuts", "Shellfish", "Dairy", "Gluten")))
               from weightCount in Gen.Choose(1, 3)
               from weights in Gen.Sequence(Enumerable.Range(0, weightCount)
                   .Select(_ => from key in Gen.Elements("protein", "carbs", "fats")
                               from value in Gen.Choose(1, 100)
                               select (key, value)))
               let mealTypesJson = $"[{string.Join(",", mealTypes.Select(m => $"\"{m}\""))}]"
               let allergiesJson = allergyCount == 0 ? "[]" : $"[{string.Join(",", allergies.Select(a => $"\"{a}\""))}]"
               let weightsJson = $"{{{string.Join(",", weights.Select(w => $"\"{w.key}\":{w.value}"))}}}"
               select new ExcelRowData
               {
                   CustomerSegment = segment,
                   PreferredMealTypes = mealTypesJson,
                   AverageCalorieTarget = calories,
                   CommonAllergies = allergiesJson,
                   RecommendationWeights = weightsJson,
                   IsValid = true
               };
    }

    /// <summary>
    /// Generate an invalid Excel row (various types of invalid data)
    /// Note: PreferredMealTypes and CommonAllergies go through EnsureJsonFormat conversion,
    /// so empty strings become "[]" which is valid. Only truly malformed JSON will fail.
    /// </summary>
    private static Gen<ExcelRowData> GenerateInvalidExcelRow()
    {
        return Gen.OneOf(
            // Missing CustomerSegment
            from mealTypes in Gen.Elements("[\"Breakfast\"]", "[\"Lunch\",\"Dinner\"]")
            from calories in Gen.Choose(100, 3000)
            from weights in Gen.Constant("{\"protein\":40,\"carbs\":30}")
            select new ExcelRowData
            {
                CustomerSegment = string.Empty,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                CommonAllergies = "[]",
                RecommendationWeights = weights,
                IsValid = false
            },
            // Negative calories
            from segment in Gen.Elements("Athletic", "WeightLoss", "Diabetic")
            from mealTypes in Gen.Elements("[\"Breakfast\"]", "[\"Lunch\"]")
            from calories in Gen.Choose(-1000, 0)
            from weights in Gen.Constant("{\"protein\":40}")
            select new ExcelRowData
            {
                CustomerSegment = segment,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                CommonAllergies = "[]",
                RecommendationWeights = weights,
                IsValid = false
            },
            // Invalid JSON in PreferredMealTypes (must start with [ or { to avoid conversion)
            from segment in Gen.Elements("Athletic", "WeightLoss", "Diabetic")
            from invalidJson in Gen.Elements("[unclosed", "{invalid json}", "[1, 2, 3,]", "{\"key\": }")
            from calories in Gen.Choose(100, 3000)
            from weights in Gen.Constant("{\"protein\":40}")
            select new ExcelRowData
            {
                CustomerSegment = segment,
                PreferredMealTypes = invalidJson,
                AverageCalorieTarget = calories,
                CommonAllergies = "[]",
                RecommendationWeights = weights,
                IsValid = false
            },
            // Invalid JSON in RecommendationWeights
            from segment in Gen.Elements("Athletic", "WeightLoss", "Diabetic")
            from mealTypes in Gen.Elements("[\"Breakfast\"]", "[\"Lunch\"]")
            from calories in Gen.Choose(100, 3000)
            from invalidJson in Gen.Elements("{invalid}", "[bad]", "not json", "{\"key\": }", "[unclosed")
            select new ExcelRowData
            {
                CustomerSegment = segment,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                CommonAllergies = "[]",
                RecommendationWeights = invalidJson,
                IsValid = false
            },
            // Missing RecommendationWeights
            from segment in Gen.Elements("Athletic", "WeightLoss", "Diabetic")
            from mealTypes in Gen.Elements("[\"Breakfast\"]", "[\"Lunch\"]")
            from calories in Gen.Choose(100, 3000)
            select new ExcelRowData
            {
                CustomerSegment = segment,
                PreferredMealTypes = mealTypes,
                AverageCalorieTarget = calories,
                CommonAllergies = "[]",
                RecommendationWeights = string.Empty,
                IsValid = false
            }
        );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create an Excel file with the specified test data
    /// </summary>
    private void CreateExcelFile(string filePath, ExcelTestData data)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("TrainingData");

        // Add headers
        worksheet.Cells[1, 1].Value = "CustomerSegment";
        worksheet.Cells[1, 2].Value = "PreferredMealTypes";
        worksheet.Cells[1, 3].Value = "AverageCalorieTarget";
        worksheet.Cells[1, 4].Value = "CommonAllergies";
        worksheet.Cells[1, 5].Value = "RecommendationWeights";

        // Add data rows
        for (int i = 0; i < data.Rows.Count; i++)
        {
            var row = data.Rows[i];
            var excelRow = i + 2; // Start from row 2 (row 1 is header)

            worksheet.Cells[excelRow, 1].Value = row.CustomerSegment;
            worksheet.Cells[excelRow, 2].Value = row.PreferredMealTypes;
            worksheet.Cells[excelRow, 3].Value = row.AverageCalorieTarget;
            worksheet.Cells[excelRow, 4].Value = row.CommonAllergies;
            worksheet.Cells[excelRow, 5].Value = row.RecommendationWeights;
        }

        package.SaveAs(new FileInfo(filePath));
    }

    #endregion

    #region Test Data Classes

    /// <summary>
    /// Represents Excel test data with multiple rows
    /// </summary>
    private class ExcelTestData
    {
        public List<ExcelRowData> Rows { get; set; } = new();
    }

    /// <summary>
    /// Represents a single Excel row with validation status
    /// </summary>
    private class ExcelRowData
    {
        public string CustomerSegment { get; set; } = string.Empty;
        public string PreferredMealTypes { get; set; } = string.Empty;
        public int AverageCalorieTarget { get; set; }
        public string CommonAllergies { get; set; } = string.Empty;
        public string RecommendationWeights { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }

    #endregion

    public void Dispose()
    {
        // Clean up test files
        foreach (var filePath in _testFilesToCleanup)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
