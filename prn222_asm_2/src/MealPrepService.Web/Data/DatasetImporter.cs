using Microsoft.EntityFrameworkCore;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using OfficeOpenXml;

namespace MealPrepService.Web.Data;

public class DatasetImporter
{
    private readonly MealPrepDbContext _context;
    private readonly string _filesDirectory;

    public DatasetImporter(MealPrepDbContext context, string filesDirectory)
    {
        _context = context;
        _filesDirectory = filesDirectory;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task ImportAllAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await ImportAllergiesAsync();
            await ImportIngredientsAsync();
            await ImportRecipesAsync();
            await ImportRecipeIngredientsAsync();

            await transaction.CommitAsync();
            Console.WriteLine("Import completed successfully!");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Import failed: {ex.Message}");
            throw;
        }
    }

    private async Task ImportAllergiesAsync()
    {
        var worksheet = OpenExcelFile("Dataset_Allergy.xlsx");
        var rowCount = worksheet.Dimension?.Rows ?? 0;
        var importedCount = 0;

        for (int row = 2; row <= rowCount; row++)
        {
            var allergyName = GetCellValue(worksheet, row, 1);
            if (string.IsNullOrWhiteSpace(allergyName))
                continue;

            var allergy = new Allergy
            {
                Id = Guid.NewGuid(),
                AllergyName = allergyName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Allergies.AddAsync(allergy);
            importedCount++;
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Imported {importedCount} allergies");
    }

    private async Task ImportIngredientsAsync()
    {
        var worksheet = OpenExcelFile("Dataset_Ingredient.xlsx");
        var rowCount = worksheet.Dimension?.Rows ?? 0;
        var importedCount = 0;

        for (int row = 2; row <= rowCount; row++)
        {
            var ingredientName = GetCellValue(worksheet, row, 1);
            if (string.IsNullOrWhiteSpace(ingredientName))
                continue;

            var ingredient = new Ingredient
            {
                Id = Guid.NewGuid(),
                IngredientName = ingredientName,
                Unit = GetCellValue(worksheet, row, 2),
                CaloPerUnit = GetFloatValue(worksheet, row, 3),
                IsAllergen = GetBoolValue(worksheet, row, 4),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Ingredients.AddAsync(ingredient);
            importedCount++;
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Imported {importedCount} ingredients");
    }

    private async Task ImportRecipesAsync()
    {
        var worksheet = OpenExcelFile("Dataset_Recipe.xlsx");
        var rowCount = worksheet.Dimension?.Rows ?? 0;
        var importedCount = 0;

        for (int row = 2; row <= rowCount; row++)
        {
            var recipeName = GetCellValue(worksheet, row, 1);
            if (string.IsNullOrWhiteSpace(recipeName))
                continue;

            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                RecipeName = recipeName,
                Instructions = GetCellValue(worksheet, row, 2),
                TotalCalories = GetFloatValue(worksheet, row, 3),
                ProteinG = GetFloatValue(worksheet, row, 4),
                FatG = GetFloatValue(worksheet, row, 5),
                CarbsG = GetFloatValue(worksheet, row, 6),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Recipes.AddAsync(recipe);
            importedCount++;
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Imported {importedCount} recipes");
    }

    private async Task ImportRecipeIngredientsAsync()
    {
        var worksheet = OpenExcelFile("Dataset_Recipe_Ingredient.xlsx");
        var rowCount = worksheet.Dimension?.Rows ?? 0;
        var importedCount = 0;

        // Load all recipes and ingredients into memory for lookup
        var recipes = await _context.Recipes.ToListAsync();
        var ingredients = await _context.Ingredients.ToListAsync();

        for (int row = 2; row <= rowCount; row++)
        {
            var recipeIdStr = GetCellValue(worksheet, row, 1);
            var ingredientIdStr = GetCellValue(worksheet, row, 2);

            if (string.IsNullOrWhiteSpace(recipeIdStr) || string.IsNullOrWhiteSpace(ingredientIdStr))
                continue;

            // Parse GUIDs
            if (!Guid.TryParse(recipeIdStr, out var recipeId) || !Guid.TryParse(ingredientIdStr, out var ingredientId))
                continue;

            var recipeIngredient = new RecipeIngredient
            {
                RecipeId = recipeId,
                IngredientId = ingredientId,
                Amount = GetFloatValue(worksheet, row, 3)
            };

            await _context.RecipeIngredients.AddAsync(recipeIngredient);
            importedCount++;
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Imported {importedCount} recipe ingredients");
    }

    private ExcelWorksheet OpenExcelFile(string fileName)
    {
        var filePath = Path.Combine(_filesDirectory, fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var fileInfo = new FileInfo(filePath);
        var package = new ExcelPackage(fileInfo);
        return package.Workbook.Worksheets[0];
    }

    private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
    {
        return worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
    }

    private float GetFloatValue(ExcelWorksheet worksheet, int row, int col)
    {
        var value = worksheet.Cells[row, col].Value;
        if (value == null) return 0f;

        if (value is double d)
            return (float)d;
        if (value is float f)
            return f;
        if (float.TryParse(value.ToString(), out var result))
            return result;

        return 0f;
    }

    private bool GetBoolValue(ExcelWorksheet worksheet, int row, int col)
    {
        var value = worksheet.Cells[row, col].Value?.ToString();
        return value == "1" || value?.ToLower() == "true";
    }
}
