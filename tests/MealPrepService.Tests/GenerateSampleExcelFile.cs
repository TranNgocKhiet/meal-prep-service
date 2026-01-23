using OfficeOpenXml;

namespace MealPrepService.Tests;

/// <summary>
/// Helper class to generate the sample PRN222_Datasets.xlsx file
/// Run this test once to create the sample file in the files directory
/// </summary>
public class GenerateSampleExcelFile
{
    [Fact(Skip = "Run manually to generate sample Excel file")]
    [Trait("Feature", "ai-meal-recommendations")]
    [Trait("Task", "2.1")]
    public void GeneratePRN222DatasetsFile()
    {
        // Set EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var projectRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..");
        var filePath = Path.Combine(projectRoot, "files", "PRN222_Datasets.xlsx");

        // Ensure files directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("TrainingData");

        // Add headers
        worksheet.Cells[1, 1].Value = "CustomerSegment";
        worksheet.Cells[1, 2].Value = "PreferredMealTypes";
        worksheet.Cells[1, 3].Value = "AverageCalorieTarget";
        worksheet.Cells[1, 4].Value = "CommonAllergies";
        worksheet.Cells[1, 5].Value = "RecommendationWeights";

        // Style headers
        using (var range = worksheet.Cells[1, 1, 1, 5])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
        }

        // Add sample data rows
        int row = 2;

        // Row 1: Athletic segment
        worksheet.Cells[row, 1].Value = "Athletic";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\",\"Snack\"]";
        worksheet.Cells[row, 3].Value = 2800;
        worksheet.Cells[row, 4].Value = "[\"Peanuts\"]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.35,\"carbs\":0.40,\"fats\":0.25,\"variety\":0.20,\"calorie_alignment\":0.15}";
        row++;

        // Row 2: Weight Loss segment
        worksheet.Cells[row, 1].Value = "Weight Loss";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\"]";
        worksheet.Cells[row, 3].Value = 1600;
        worksheet.Cells[row, 4].Value = "[]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.40,\"carbs\":0.30,\"fats\":0.30,\"variety\":0.25,\"calorie_alignment\":0.20}";
        row++;

        // Row 3: Diabetic segment
        worksheet.Cells[row, 1].Value = "Diabetic";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\"]";
        worksheet.Cells[row, 3].Value = 2000;
        worksheet.Cells[row, 4].Value = "[\"Sugar\",\"High Fructose Corn Syrup\"]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.30,\"carbs\":0.45,\"fats\":0.25,\"variety\":0.15,\"calorie_alignment\":0.25}";
        row++;

        // Row 4: Muscle Gain segment
        worksheet.Cells[row, 1].Value = "Muscle Gain";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\",\"Snack\",\"Post-Workout\"]";
        worksheet.Cells[row, 3].Value = 3200;
        worksheet.Cells[row, 4].Value = "[\"Shellfish\"]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.40,\"carbs\":0.35,\"fats\":0.25,\"variety\":0.20,\"calorie_alignment\":0.15}";
        row++;

        // Row 5: Vegetarian segment
        worksheet.Cells[row, 1].Value = "Vegetarian";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\"]";
        worksheet.Cells[row, 3].Value = 2200;
        worksheet.Cells[row, 4].Value = "[\"Eggs\",\"Dairy\"]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.30,\"carbs\":0.45,\"fats\":0.25,\"variety\":0.30,\"calorie_alignment\":0.15}";
        row++;

        // Row 6: Vegan segment
        worksheet.Cells[row, 1].Value = "Vegan";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\",\"Snack\"]";
        worksheet.Cells[row, 3].Value = 2100;
        worksheet.Cells[row, 4].Value = "[\"Eggs\",\"Dairy\",\"Honey\"]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.30,\"carbs\":0.45,\"fats\":0.25,\"variety\":0.35,\"calorie_alignment\":0.15}";
        row++;

        // Row 7: Heart Health segment
        worksheet.Cells[row, 1].Value = "Heart Health";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\"]";
        worksheet.Cells[row, 3].Value = 1900;
        worksheet.Cells[row, 4].Value = "[]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.30,\"carbs\":0.40,\"fats\":0.30,\"variety\":0.20,\"calorie_alignment\":0.20}";
        row++;

        // Row 8: Keto segment
        worksheet.Cells[row, 1].Value = "Keto";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\"]";
        worksheet.Cells[row, 3].Value = 2000;
        worksheet.Cells[row, 4].Value = "[\"Gluten\"]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.25,\"carbs\":0.10,\"fats\":0.65,\"variety\":0.20,\"calorie_alignment\":0.15}";
        row++;

        // Row 9: Balanced Diet segment
        worksheet.Cells[row, 1].Value = "Balanced Diet";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\",\"Snack\"]";
        worksheet.Cells[row, 3].Value = 2200;
        worksheet.Cells[row, 4].Value = "[]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.30,\"carbs\":0.40,\"fats\":0.30,\"variety\":0.25,\"calorie_alignment\":0.15}";
        row++;

        // Row 10: Senior Health segment
        worksheet.Cells[row, 1].Value = "Senior Health";
        worksheet.Cells[row, 2].Value = "[\"Breakfast\",\"Lunch\",\"Dinner\"]";
        worksheet.Cells[row, 3].Value = 1800;
        worksheet.Cells[row, 4].Value = "[\"Nuts\"]";
        worksheet.Cells[row, 5].Value = "{\"protein\":0.35,\"carbs\":0.35,\"fats\":0.30,\"variety\":0.20,\"calorie_alignment\":0.20}";

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Save the file
        package.SaveAs(new FileInfo(filePath));

        Assert.True(File.Exists(filePath), $"File should be created at {filePath}");
    }
}
