using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class DatasetImportService : IDatasetImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IExcelReaderService _excelReader;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatasetImportService> _logger;
    private readonly string _datasetFolderPath;
    private const string ImportCompletionKey = "DatasetImportCompleted";

    // ID mappings from Excel GUIDs to database GUIDs
    private Dictionary<string, string> _recipeIdMapping = new();
    private Dictionary<string, string> _ingredientIdMapping = new();
    private Dictionary<string, string> _nutrientIdMapping = new();
    private Dictionary<string, string> _allergyIdMapping = new();

    public DatasetImportService(
        ApplicationDbContext context,
        IExcelReaderService excelReader,
        IConfiguration configuration,
        ILogger<DatasetImportService> logger)
    {
        _context = context;
        _excelReader = excelReader;
        _configuration = configuration;
        _logger = logger;
        
        // Get the base directory and construct the dataset path
        // From: src/MealPreparationService.API/bin/Debug/net9.0
        // To: repository root (where document folder is)
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        // Navigate up from bin/Debug/net9.0 to API project, then to src, then to solution root
        var solutionRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "..", ".."));
        _datasetFolderPath = Path.Combine(solutionRoot, "document", "dataset");
        
        _logger.LogInformation("Dataset folder path: {Path}", _datasetFolderPath);
    }

    public async Task<bool> ShouldImportDataAsync()
    {
        try
        {
            var importFlag = await _context.SystemConfigurations
                .FirstOrDefaultAsync(sc => sc.Key == ImportCompletionKey);

            if (importFlag != null && importFlag.Value == "true")
            {
                _logger.LogInformation("Dataset import has already been completed");
                return false;
            }

            // Check if we have any recipes (main data indicator)
            var hasRecipes = await _context.Recipes.AnyAsync();
            
            if (hasRecipes)
            {
                _logger.LogInformation("Recipes table already contains data, skipping import");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if dataset import should run");
            return false;
        }
    }

    public async Task<ImportResultDto> ImportAllDatasetsAsync()
    {
        var result = new ImportResultDto
        {
            ImportedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting dataset import process");

            // Import in the correct order as specified in requirements
            var importTasks = new (string fileName, Func<string, Task<int>> importFunc)[]
            {
                ("Roles.xlsx", ImportRolesAsync),
                ("Statuses.xlsx", ImportStatusAsync),
                ("MealType.xlsx", ImportMealTypesAsync),
                ("RelationshipType.xlsx", ImportRelationshipTypesAsync),
                ("SystemConfigurations.xlsx", ImportSystemConfigurationsAsync),
                ("Nutrients.xlsx", ImportNutrientsAsync),
                ("Allergies.xlsx", ImportAllergiesAsync),
                ("Ingredients.xlsx", ImportIngredientsAsync),
                ("Recipes.xlsx", ImportRecipesAsync),
                ("RecipeIngredients.xlsx", ImportRecipeIngredientsAsync),
                ("IngredientAllergies.xlsx", ImportIngredientAllergiesAsync),
                ("IngredientNutrients.xlsx", ImportIngredientNutrientsAsync)
            };

            foreach (var (fileName, importFunc) in importTasks)
            {
                try
                {
                    var filePath = Path.Combine(_datasetFolderPath, fileName);
                    var count = await importFunc(filePath);
                    result.ImportedFiles.Add(fileName);
                    result.TotalRecordsImported += count;
                    _logger.LogInformation("Successfully imported {Count} records from {FileName}", count, fileName);
                }
                catch (Exception ex)
                {
                    result.FailedFiles.Add(fileName);
                    result.Errors.Add($"{fileName}: {ex.Message}");
                    _logger.LogError(ex, "Failed to import {FileName}", fileName);
                }
            }

            // Validate data integrity
            var validationErrors = await ValidateDataIntegrityAsync();
            if (validationErrors.Any())
            {
                result.Warnings.AddRange(validationErrors);
                _logger.LogWarning("Data integrity validation found {Count} issues", validationErrors.Count);
            }

            // Set import completion flag
            if (result.FailedFiles.Count == 0)
            {
                await SetImportCompletionFlagAsync();
                result.Success = true;
                _logger.LogInformation("Dataset import completed successfully. Total records: {Count}", result.TotalRecordsImported);
            }
            else
            {
                result.Success = false;
                _logger.LogWarning("Dataset import completed with errors. Failed files: {Count}", result.FailedFiles.Count);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Import process failed: {ex.Message}");
            _logger.LogError(ex, "Dataset import process failed");
        }

        return result;
    }

    private async Task<int> ImportRolesAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var roles = new List<Role>();

        // Get existing roles
        var existingRoles = await _context.Roles.ToListAsync();
        var existingRoleNames = existingRoles.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Create a mapping from Excel ID to GUID for later use
        var roleIdMapping = new Dictionary<int, string>();

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (roles.Count == 0)
            {
                _logger.LogInformation("Available columns in Roles.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var name = GetColumnValue(row, "RoleName", "role_name", "Name");
            var excelIdStr = GetColumnValue(row, "Id", "role_id", "RoleId", "Role ID");
            
            // Skip rows with empty names to avoid duplicate key errors
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Skipping role with empty name");
                continue;
            }

            // Skip if role already exists
            if (existingRoleNames.Contains(name))
            {
                _logger.LogInformation("Role '{Name}' already exists, skipping", name);
                // Find existing role ID for mapping
                var existingRole = existingRoles.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existingRole != null && int.TryParse(excelIdStr, out var excelId))
                {
                    roleIdMapping[excelId] = existingRole.Id.ToString();
                }
                continue;
            }

            // Generate sequential int ID
            var roleId = existingRoles.Count + roles.Count + 1;
            var role = new Role
            {
                Id = roleId,
                Name = name
            };
            roles.Add(role);

            // Store mapping if Excel ID is available
            if (int.TryParse(excelIdStr, out var excelIdParsed))
            {
                roleIdMapping[excelIdParsed] = roleId.ToString();
            }
        }

        if (roles.Count > 0)
        {
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();
        }
        
        return roles.Count;
    }

    private async Task<int> ImportStatusAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var statuses = new List<Status>();

        // Get existing statuses
        var existingStatuses = await _context.Statuses.ToListAsync();
        var existingStatusNames = existingStatuses.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (statuses.Count == 0)
            {
                _logger.LogInformation("Available columns in Status.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var name = GetColumnValue(row, "StatusName", "status_name", "Name");
            
            // Skip rows with empty names
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Skipping status with empty name");
                continue;
            }

            // Skip if status already exists
            if (existingStatusNames.Contains(name))
            {
                _logger.LogInformation("Status '{Name}' already exists, skipping", name);
                continue;
            }

            // Generate sequential int ID
            var statusId = existingStatuses.Count + statuses.Count + 1;
            var status = new Status
            {
                Id = statusId,
                Name = name
            };
            statuses.Add(status);
        }

        if (statuses.Count > 0)
        {
            await _context.Statuses.AddRangeAsync(statuses);
            await _context.SaveChangesAsync();
        }
        
        return statuses.Count;
    }

    private async Task<int> ImportMealTypesAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var mealTypes = new List<MealType>();

        // Get existing meal types
        var existingMealTypes = await _context.MealTypes.ToListAsync();
        var existingMealTypeNames = existingMealTypes.Select(mt => mt.TypeName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (mealTypes.Count == 0)
            {
                _logger.LogInformation("Available columns in MealType.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var typeName = GetColumnValue(row, "TypeName", "type_name", "Name");
            
            // Skip rows with empty names
            if (string.IsNullOrWhiteSpace(typeName))
            {
                _logger.LogWarning("Skipping meal type with empty name");
                continue;
            }

            // Skip if meal type already exists
            if (existingMealTypeNames.Contains(typeName))
            {
                _logger.LogInformation("MealType '{Name}' already exists, skipping", typeName);
                continue;
            }

            // Generate sequential int ID: 1=Breakfast, 2=Lunch, 3=Dinner
            var mealTypeId = existingMealTypes.Count + mealTypes.Count + 1;
            var mealType = new MealType
            {
                Id = mealTypeId,
                TypeName = typeName
            };
            mealTypes.Add(mealType);
        }

        if (mealTypes.Count > 0)
        {
            await _context.MealTypes.AddRangeAsync(mealTypes);
            await _context.SaveChangesAsync();
        }
        
        return mealTypes.Count;
    }

    private async Task<int> ImportRelationshipTypesAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var relationshipTypes = new List<RelationshipType>();

        // Get existing relationship types
        var existingRelationshipTypes = await _context.RelationshipTypes.ToListAsync();
        var existingRelationshipTypeNames = existingRelationshipTypes.Select(rt => rt.TypeName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (relationshipTypes.Count == 0)
            {
                _logger.LogInformation("Available columns in RelationshipType.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var typeName = GetColumnValue(row, "TypeName", "type_name", "Name");
            
            // Skip rows with empty names
            if (string.IsNullOrWhiteSpace(typeName))
            {
                _logger.LogWarning("Skipping relationship type with empty name");
                continue;
            }

            // Skip if relationship type already exists
            if (existingRelationshipTypeNames.Contains(typeName))
            {
                _logger.LogInformation("RelationshipType '{Name}' already exists, skipping", typeName);
                continue;
            }

            // Generate sequential int ID: 1=Like, 2=Dislike, 3=Allergen
            var relationshipTypeId = existingRelationshipTypes.Count + relationshipTypes.Count + 1;
            var relationshipType = new RelationshipType
            {
                Id = relationshipTypeId,
                TypeName = typeName
            };
            relationshipTypes.Add(relationshipType);
        }

        if (relationshipTypes.Count > 0)
        {
            await _context.RelationshipTypes.AddRangeAsync(relationshipTypes);
            await _context.SaveChangesAsync();
        }
        
        return relationshipTypes.Count;
    }

    private async Task<int> ImportSystemConfigurationsAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var systemConfigurations = new List<SystemConfiguration>();

        // Get existing system configurations
        var existingConfigs = await _context.SystemConfigurations.ToListAsync();
        var existingConfigKeys = existingConfigs.Select(sc => sc.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (systemConfigurations.Count == 0)
            {
                _logger.LogInformation("Available columns in SystemConfigurations.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var key = GetColumnValue(row, "Key", "key", "ConfigKey");
            var value = GetColumnValue(row, "Value", "value", "ConfigValue");
            var dataType = GetColumnValue(row, "DataType", "data_type", "Type");
            var description = GetColumnValue(row, "Description", "description", "Desc");
            
            // Skip rows with empty keys
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Skipping system configuration with empty key");
                continue;
            }

            // Skip if configuration already exists
            if (existingConfigKeys.Contains(key))
            {
                _logger.LogInformation("SystemConfiguration '{Key}' already exists, skipping", key);
                continue;
            }

            var systemConfiguration = new SystemConfiguration
            {
                Id = Guid.NewGuid().ToString(),
                Key = key,
                Value = value ?? string.Empty,
                DataType = dataType ?? "String",
                Description = description ?? string.Empty,
                UpdatedAt = DateTime.UtcNow
            };
            systemConfigurations.Add(systemConfiguration);
        }

        if (systemConfigurations.Count > 0)
        {
            await _context.SystemConfigurations.AddRangeAsync(systemConfigurations);
            await _context.SaveChangesAsync();
        }
        
        return systemConfigurations.Count;
    }

    private async Task<int> ImportNutrientsAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var nutrients = new List<Nutrient>();

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (nutrients.Count == 0)
            {
                _logger.LogInformation("Available columns in Nutrients.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var name = GetColumnValue(row, "NutrientName", "nutrient_name", "Name");
            var excelIdStr = GetColumnValue(row, "Id", "nutrient_id", "NutrientId", "Nutrient ID", "ID", "id");
            
            // DEBUG: Log what we're reading
            if (nutrients.Count < 3)
            {
                _logger.LogInformation("DEBUG Nutrient row {Index}: excelIdStr='{ExcelId}', name='{Name}'", nutrients.Count + 1, excelIdStr ?? "NULL", name ?? "NULL");
            }
            
            // Skip rows with empty names
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Skipping nutrient with empty name (excelId: {ExcelId})", excelIdStr ?? "NULL");
                continue;
            }

            var guid = Guid.NewGuid().ToString();
            var nutrient = new Nutrient
            {
                Id = guid,
                Name = name
            };
            nutrients.Add(nutrient);

            // Store mapping
            if (!string.IsNullOrWhiteSpace(excelIdStr) && Guid.TryParse(excelIdStr, out var excelGuid))
            {
                _nutrientIdMapping[excelIdStr] = guid;
                if (nutrients.Count <= 3)
                {
                    _logger.LogInformation("DEBUG: Added nutrient mapping {ExcelId} -> {Guid}", excelIdStr, guid.Substring(0, 8));
                }
            }
            else
            {
                _logger.LogWarning("Could not parse nutrient Excel ID: '{ExcelId}' for nutrient '{Name}'", excelIdStr ?? "NULL", name);
            }
        }

        _logger.LogInformation("Nutrient ID mapping final size: {Count} mappings created", _nutrientIdMapping.Count);
        if (_nutrientIdMapping.Count > 0)
        {
            _logger.LogInformation("Sample nutrient mappings: {Samples}", string.Join(", ", _nutrientIdMapping.Take(5).Select(kvp => $"{kvp.Key}->{kvp.Value.Substring(0, 8)}")));
        }

        await _context.Nutrients.AddRangeAsync(nutrients);
        await _context.SaveChangesAsync();
        
        return nutrients.Count;
    }

    private async Task<int> ImportAllergiesAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var allergies = new List<Allergy>();

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (allergies.Count == 0)
            {
                _logger.LogInformation("Available columns in Allergies.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var name = GetColumnValue(row, "AllergyName", "allergy_name", "Name");
            var excelIdStr = GetColumnValue(row, "Id", "allergy_id", "AllergyId", "Allergy ID", "ID", "id");
            
            // DEBUG: Log what we're reading
            if (allergies.Count < 3)
            {
                _logger.LogInformation("DEBUG Allergy row {Index}: excelIdStr='{ExcelId}', name='{Name}'", allergies.Count + 1, excelIdStr ?? "NULL", name ?? "NULL");
            }
            
            // Skip rows with empty names
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Skipping allergy with empty name (excelId: {ExcelId})", excelIdStr ?? "NULL");
                continue;
            }

            var guid = Guid.NewGuid().ToString();
            var allergy = new Allergy
            {
                Id = guid,
                Name = name
            };
            allergies.Add(allergy);

            // Store mapping
            if (!string.IsNullOrWhiteSpace(excelIdStr) && Guid.TryParse(excelIdStr, out var excelGuid))
            {
                _allergyIdMapping[excelIdStr] = guid;
                if (allergies.Count <= 3)
                {
                    _logger.LogInformation("DEBUG: Added allergy mapping {ExcelId} -> {Guid}", excelIdStr, guid.Substring(0, 8));
                }
            }
            else
            {
                _logger.LogWarning("Could not parse allergy Excel ID: '{ExcelId}' for allergy '{Name}'", excelIdStr ?? "NULL", name);
            }
        }

        _logger.LogInformation("Allergy ID mapping final size: {Count} mappings created", _allergyIdMapping.Count);
        if (_allergyIdMapping.Count > 0)
        {
            _logger.LogInformation("Sample allergy mappings: {Samples}", string.Join(", ", _allergyIdMapping.Take(5).Select(kvp => $"{kvp.Key}->{kvp.Value.Substring(0, 8)}")));
        }

        await _context.Allergies.AddRangeAsync(allergies);
        await _context.SaveChangesAsync();
        
        return allergies.Count;
    }

    private async Task<int> ImportIngredientsAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var ingredients = new List<Ingredient>();

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (ingredients.Count == 0)
            {
                _logger.LogInformation("Available columns in Ingredients.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            var name = GetColumnValue(row, "IngredientName", "ingredient_name", "Name", "Ingredient Name");
            var excelIdStr = GetColumnValue(row, "Id", "ingredient_id", "IngredientId", "Ingredient ID", "ID", "id");
            
            // DEBUG: Log what we're reading
            if (ingredients.Count < 3)
            {
                _logger.LogInformation("DEBUG Ingredient row {Index}: excelIdStr='{ExcelId}', name='{Name}'", ingredients.Count + 1, excelIdStr ?? "NULL", name ?? "NULL");
            }
            
            // Skip rows with empty names
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Skipping ingredient with empty name (excelId: {ExcelId})", excelIdStr ?? "NULL");
                continue;
            }

            var guid = Guid.NewGuid().ToString();
            var ingredient = new Ingredient
            {
                Id = guid,
                Name = name,
                Unit = GetColumnValue(row, "Unit", "unit") ?? string.Empty,
                ImageUrl = GetColumnValue(row, "ImageUrl", "image_url", "Image URL", "Image") ?? string.Empty
            };
            ingredients.Add(ingredient);

            // Store mapping
            if (!string.IsNullOrWhiteSpace(excelIdStr) && Guid.TryParse(excelIdStr, out var excelGuid))
            {
                _ingredientIdMapping[excelIdStr] = guid;
                if (ingredients.Count <= 3)
                {
                    _logger.LogInformation("DEBUG: Added ingredient mapping {ExcelId} -> {Guid}", excelIdStr, guid.Substring(0, 8));
                }
            }
            else
            {
                _logger.LogWarning("Could not parse ingredient Excel ID: '{ExcelId}' for ingredient '{Name}'", excelIdStr ?? "NULL", name);
            }
        }

        _logger.LogInformation("Ingredient ID mapping final size: {Count} mappings created", _ingredientIdMapping.Count);
        if (_ingredientIdMapping.Count > 0)
        {
            _logger.LogInformation("Sample ingredient mappings: {Samples}", string.Join(", ", _ingredientIdMapping.Take(5).Select(kvp => $"{kvp.Key}->{kvp.Value.Substring(0, 8)}")));
        }

        await _context.Ingredients.AddRangeAsync(ingredients);
        await _context.SaveChangesAsync();
        
        return ingredients.Count;
    }

    private async Task<int> ImportRecipesAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var recipes = new List<Recipe>();

        foreach (var row in rows)
        {
            // Log available columns for debugging
            if (recipes.Count == 0)
            {
                _logger.LogInformation("Available columns in Recipes.xlsx: {Columns}", string.Join(", ", row.Keys));
            }

            // Use RecipeName for Name field
            var name = GetColumnValue(row, "RecipeName", "recipe_name", "Name", "Recipe Name");
            var excelIdStr = GetColumnValue(row, "Id", "recipe_id", "RecipeId", "Recipe ID", "ID", "id");
            
            // DEBUG: Log what we're reading
            if (recipes.Count < 3)
            {
                _logger.LogInformation("DEBUG Recipe row {Index}: excelIdStr='{ExcelId}', name='{Name}'", recipes.Count + 1, excelIdStr ?? "NULL", name ?? "NULL");
            }
            
            // Skip rows with empty names
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("Skipping recipe with empty name (excelId: {ExcelId})", excelIdStr ?? "NULL");
                continue;
            }

            var guid = Guid.NewGuid().ToString();
            var recipe = new Recipe
            {
                Id = guid,
                RecipeName = name,
                // Use Instructions column for Instructions field
                Instructions = GetColumnValue(row, "Instructions", "instructions", "Description") ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            recipes.Add(recipe);

            // Store mapping
            if (!string.IsNullOrWhiteSpace(excelIdStr) && Guid.TryParse(excelIdStr, out var excelGuid))
            {
                _recipeIdMapping[excelIdStr] = guid;
                if (recipes.Count <= 3)
                {
                    _logger.LogInformation("DEBUG: Added recipe mapping {ExcelId} -> {Guid}", excelIdStr, guid.Substring(0, 8));
                }
            }
            else
            {
                _logger.LogWarning("Could not parse recipe Excel ID: '{ExcelId}' for recipe '{Name}'", excelIdStr ?? "NULL", name);
            }
        }

        _logger.LogInformation("Recipe ID mapping final size: {Count} mappings created", _recipeIdMapping.Count);
        if (_recipeIdMapping.Count > 0)
        {
            _logger.LogInformation("Sample recipe mappings: {Samples}", string.Join(", ", _recipeIdMapping.Take(5).Select(kvp => $"{kvp.Key}->{kvp.Value.Substring(0, 8)}")));
        }

        await _context.Recipes.AddRangeAsync(recipes);
        await _context.SaveChangesAsync();
        
        return recipes.Count;
    }

    private async Task<int> ImportRecipeIngredientsAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var recipeIngredients = new List<RecipeIngredient>();

        // Log available columns for debugging
        if (rows.Count > 0)
        {
            _logger.LogInformation("Available columns in RecipeIngredients.xlsx: {Columns}", string.Join(", ", rows[0].Keys));
        }

        // Log mapping sizes
        _logger.LogInformation("Recipe ID mapping size: {Count}", _recipeIdMapping.Count);
        _logger.LogInformation("Ingredient ID mapping size: {Count}", _ingredientIdMapping.Count);
        
        if (_recipeIdMapping.Count > 0)
        {
            _logger.LogInformation("Sample recipe IDs in mapping: {Samples}", string.Join(", ", _recipeIdMapping.Keys.Take(10)));
        }
        if (_ingredientIdMapping.Count > 0)
        {
            _logger.LogInformation("Sample ingredient IDs in mapping: {Samples}", string.Join(", ", _ingredientIdMapping.Keys.Take(10)));
        }

        int rowIndex = 0;
        foreach (var row in rows)
        {
            rowIndex++;
            var recipeIdStr = GetColumnValue(row, "RecipeId", "recipe_id", "Recipe ID", "RecipeID");
            var ingredientIdStr = GetColumnValue(row, "IngredientId", "ingredient_id", "Ingredient ID", "IngredientID");

            // DEBUG: Log first few rows
            if (rowIndex <= 3)
            {
                _logger.LogInformation("DEBUG RecipeIngredient row {Index}: recipeIdStr='{RecipeId}', ingredientIdStr='{IngredientId}'", 
                    rowIndex, recipeIdStr ?? "NULL", ingredientIdStr ?? "NULL");
            }

            if (string.IsNullOrWhiteSpace(recipeIdStr) || string.IsNullOrWhiteSpace(ingredientIdStr))
            {
                _logger.LogWarning("Skipping RecipeIngredient row {Index} with empty recipe or ingredient ID", rowIndex);
                continue;
            }

            // Validate GUIDs
            if (!Guid.TryParse(recipeIdStr, out _) || !Guid.TryParse(ingredientIdStr, out _))
            {
                _logger.LogWarning("Invalid recipe or ingredient ID format at row {Index}: recipe='{RecipeId}', ingredient='{IngredientId}'", 
                    rowIndex, recipeIdStr, ingredientIdStr);
                continue;
            }

            // Look up GUIDs using the mappings
            if (!_recipeIdMapping.TryGetValue(recipeIdStr, out var recipeGuid))
            {
                _logger.LogWarning("Recipe ID {RecipeId} not found in mapping at row {Index}. Available keys: {Keys}", 
                    recipeIdStr, rowIndex, string.Join(", ", _recipeIdMapping.Keys.Take(10).Select(k => k.Substring(0, 8))));
                continue;
            }

            if (!_ingredientIdMapping.TryGetValue(ingredientIdStr, out var ingredientGuid))
            {
                _logger.LogWarning("Ingredient ID {IngredientId} not found in mapping at row {Index}. Available keys: {Keys}", 
                    ingredientIdStr, rowIndex, string.Join(", ", _ingredientIdMapping.Keys.Take(10).Select(k => k.Substring(0, 8))));
                continue;
            }

            var qtyStr = GetColumnValue(row, "Amount", "amount", "quantity", "Quantity");
            var optionalStr = GetColumnValue(row, "is_optional", "IsOptional", "Is Optional", "Optional");

            var recipeIngredient = new RecipeIngredient
            {
                Id = Guid.NewGuid().ToString(),
                RecipeId = recipeGuid,
                IngredientId = ingredientGuid,
                Amount = decimal.TryParse(qtyStr, out var qty) ? qty : 0
            };
            recipeIngredients.Add(recipeIngredient);
            
            if (rowIndex <= 3)
            {
                _logger.LogInformation("DEBUG: Successfully created RecipeIngredient for recipe {RecipeId} and ingredient {IngredientId}", 
                    recipeIdStr.Substring(0, 8), ingredientIdStr.Substring(0, 8));
            }
        }

        _logger.LogInformation("Created {Count} RecipeIngredient records from {TotalRows} rows", recipeIngredients.Count, rows.Count);

        await _context.RecipeIngredients.AddRangeAsync(recipeIngredients);
        await _context.SaveChangesAsync();
        
        return recipeIngredients.Count;
    }

    private async Task<int> ImportIngredientAllergiesAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var ingredientAllergies = new List<IngredientAllergy>();

        // Log available columns for debugging
        if (rows.Count > 0)
        {
            _logger.LogInformation("Available columns in IngredientAllergies.xlsx: {Columns}", string.Join(", ", rows[0].Keys));
        }

        foreach (var row in rows)
        {
            var ingredientIdStr = GetColumnValue(row, "IngredientId", "ingredient_id", "Ingredient ID");
            var allergyIdStr = GetColumnValue(row, "AllergyId", "allergy_id", "Allergy ID");

            if (string.IsNullOrWhiteSpace(ingredientIdStr) || string.IsNullOrWhiteSpace(allergyIdStr))
            {
                _logger.LogWarning("Skipping IngredientAllergy with empty ingredient or allergy ID");
                continue;
            }

            // Validate GUIDs
            if (!Guid.TryParse(ingredientIdStr, out _) || !Guid.TryParse(allergyIdStr, out _))
            {
                _logger.LogWarning("Invalid ingredient or allergy ID format: ingredient={IngredientId}, allergy={AllergyId}", ingredientIdStr, allergyIdStr);
                continue;
            }

            // Look up GUIDs using the mappings
            if (!_ingredientIdMapping.TryGetValue(ingredientIdStr, out var ingredientGuid))
            {
                _logger.LogWarning("Ingredient ID not found in mapping: {IngredientId}", ingredientIdStr.Substring(0, 8));
                continue;
            }

            if (!_allergyIdMapping.TryGetValue(allergyIdStr, out var allergyGuid))
            {
                _logger.LogWarning("Allergy ID not found in mapping: {AllergyId}", allergyIdStr.Substring(0, 8));
                continue;
            }

            var ingredientAllergy = new IngredientAllergy
            {
                Id = Guid.NewGuid().ToString(),
                IngredientId = ingredientGuid,
                AllergyId = allergyGuid
            };
            ingredientAllergies.Add(ingredientAllergy);
        }

        await _context.IngredientAllergies.AddRangeAsync(ingredientAllergies);
        await _context.SaveChangesAsync();
        
        return ingredientAllergies.Count;
    }

    private async Task<int> ImportIngredientNutrientsAsync(string filePath)
    {
        var rows = await _excelReader.ReadExcelFileAsync(filePath);
        var ingredientNutrients = new List<IngredientNutrient>();

        // Log available columns for debugging
        if (rows.Count > 0)
        {
            _logger.LogInformation("Available columns in IngredientNutrients.xlsx: {Columns}", string.Join(", ", rows[0].Keys));
        }

        foreach (var row in rows)
        {
            var ingredientIdStr = GetColumnValue(row, "IngredientId", "ingredient_id", "Ingredient ID");
            var nutrientIdStr = GetColumnValue(row, "NutrientId", "nutrient_id", "Nutrient ID");

            if (string.IsNullOrWhiteSpace(ingredientIdStr) || string.IsNullOrWhiteSpace(nutrientIdStr))
            {
                _logger.LogWarning("Skipping IngredientNutrient with empty ingredient or nutrient ID");
                continue;
            }

            // Validate GUIDs
            if (!Guid.TryParse(ingredientIdStr, out _) || !Guid.TryParse(nutrientIdStr, out _))
            {
                _logger.LogWarning("Invalid ingredient or nutrient ID format: ingredient={IngredientId}, nutrient={NutrientId}", ingredientIdStr, nutrientIdStr);
                continue;
            }

            // Look up GUIDs using the mappings
            if (!_ingredientIdMapping.TryGetValue(ingredientIdStr, out var ingredientGuid))
            {
                _logger.LogWarning("Ingredient ID not found in mapping: {IngredientId}", ingredientIdStr.Substring(0, 8));
                continue;
            }

            if (!_nutrientIdMapping.TryGetValue(nutrientIdStr, out var nutrientGuid))
            {
                _logger.LogWarning("Nutrient ID not found in mapping: {NutrientId}", nutrientIdStr.Substring(0, 8));
                continue;
            }

            var amountStr = GetColumnValue(row, "AmountPer100", "amount_per_100", "amount_per_100g", "Amount Per 100", "Amount");

            var ingredientNutrient = new IngredientNutrient
            {
                Id = Guid.NewGuid().ToString(),
                IngredientId = ingredientGuid,
                NutrientId = nutrientGuid,
                AmountPer100 = decimal.TryParse(amountStr, out var amount) ? amount : 0
            };
            ingredientNutrients.Add(ingredientNutrient);
        }

        await _context.IngredientNutrients.AddRangeAsync(ingredientNutrients);
        await _context.SaveChangesAsync();
        
        return ingredientNutrients.Count;
    }

    public async Task<List<string>> ValidateDataIntegrityAsync()
    {
        var errors = new List<string>();

        try
        {
            // Check for orphaned RecipeIngredients
            var orphanedRecipeIngredients = await _context.RecipeIngredients
                .Where(ri => !_context.Recipes.Any(r => r.Id == ri.RecipeId) ||
                            !_context.Ingredients.Any(i => i.Id == ri.IngredientId))
                .CountAsync();
            
            if (orphanedRecipeIngredients > 0)
            {
                errors.Add($"Found {orphanedRecipeIngredients} orphaned RecipeIngredient records");
            }

            // Check for orphaned IngredientAllergies
            var orphanedIngredientAllergies = await _context.IngredientAllergies
                .Where(ia => !_context.Ingredients.Any(i => i.Id == ia.IngredientId) ||
                            !_context.Allergies.Any(a => a.Id == ia.AllergyId))
                .CountAsync();
            
            if (orphanedIngredientAllergies > 0)
            {
                errors.Add($"Found {orphanedIngredientAllergies} orphaned IngredientAllergy records");
            }

            // Check for orphaned IngredientNutrients
            var orphanedIngredientNutrients = await _context.IngredientNutrients
                .Where(in_entity => !_context.Ingredients.Any(i => i.Id == in_entity.IngredientId) ||
                                   !_context.Nutrients.Any(n => n.Id == in_entity.NutrientId))
                .CountAsync();
            
            if (orphanedIngredientNutrients > 0)
            {
                errors.Add($"Found {orphanedIngredientNutrients} orphaned IngredientNutrient records");
            }

            // Check for recipes without ingredients
            var recipesWithoutIngredients = await _context.Recipes
                .Where(r => !_context.RecipeIngredients.Any(ri => ri.RecipeId == r.Id))
                .CountAsync();
            
            if (recipesWithoutIngredients > 0)
            {
                errors.Add($"Found {recipesWithoutIngredients} recipes without ingredients");
            }

            _logger.LogInformation("Data integrity validation completed. Found {Count} issues", errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data integrity validation");
            errors.Add($"Validation error: {ex.Message}");
        }

        return errors;
    }

    private async Task SetImportCompletionFlagAsync()
    {
        var importFlag = await _context.SystemConfigurations
            .FirstOrDefaultAsync(sc => sc.Key == ImportCompletionKey);

        if (importFlag == null)
        {
            importFlag = new SystemConfiguration
            {
                Id = Guid.NewGuid().ToString(),
                Key = ImportCompletionKey,
                Value = "true",
                DataType = "Boolean",
                Description = "Indicates whether the initial dataset import has been completed",
                UpdatedAt = DateTime.UtcNow
            };
            await _context.SystemConfigurations.AddAsync(importFlag);
        }
        else
        {
            importFlag.Value = "true";
            importFlag.UpdatedAt = DateTime.UtcNow;
            _context.SystemConfigurations.Update(importFlag);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Import completion flag set successfully");
    }

    /// <summary>
    /// Helper method to get column value with case-insensitive and whitespace-trimmed matching
    /// Tries multiple possible column names
    /// </summary>
    private string? GetColumnValue(Dictionary<string, object> row, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            // Try exact match first
            if (row.TryGetValue(name, out var value))
            {
                return value?.ToString()?.Trim();
            }

            // Try case-insensitive match with trimmed keys
            var matchingKey = row.Keys.FirstOrDefault(k => 
                string.Equals(k?.Trim(), name, StringComparison.OrdinalIgnoreCase));
            
            if (matchingKey != null && row.TryGetValue(matchingKey, out value))
            {
                return value?.ToString()?.Trim();
            }
        }

        return null;
    }
}
