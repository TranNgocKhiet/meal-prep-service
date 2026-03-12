#!/usr/bin/env dotnet-script
#r "nuget: EPPlus, 8.5.0"

using OfficeOpenXml;
using System.IO;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var datasetPath = Path.Combine(Directory.GetCurrentDirectory(), "document", "dataset");
var files = new[] {
    "Roles.xlsx",
    "Status.xlsx",
    "Nutrients.xlsx",
    "Allergies.xlsx",
    "Ingredients.xlsx",
    "Recipes.xlsx",
    "RecipeIngredients.xlsx",
    "IngredientAllergies.xlsx",
    "IngredientNutrients.xlsx"
};

foreach (var file in files)
{
    var filePath = Path.Combine(datasetPath, file);
    if (!File.Exists(filePath))
    {
        Console.WriteLine($"File not found: {file}");
        continue;
    }

    using var package = new ExcelPackage(new FileInfo(filePath));
    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
    
    if (worksheet == null || worksheet.Dimension == null)
    {
        Console.WriteLine($"{file}: Empty or no worksheets");
        continue;
    }

    Console.WriteLine($"\n{file}:");
    Console.WriteLine("Columns:");
    
    var start = worksheet.Dimension.Start;
    var end = worksheet.Dimension.End;
    
    for (int col = start.Column; col <= end.Column; col++)
    {
        var headerValue = worksheet.Cells[start.Row, col].Value?.ToString() ?? "";
        Console.WriteLine($"  [{col}] '{headerValue}'");
    }
    
    // Show first data row as sample
    if (end.Row > start.Row)
    {
        Console.WriteLine("First data row:");
        for (int col = start.Column; col <= end.Column; col++)
        {
            var value = worksheet.Cells[start.Row + 1, col].Value?.ToString() ?? "";
            Console.WriteLine($"  [{col}] '{value}'");
        }
    }
}
