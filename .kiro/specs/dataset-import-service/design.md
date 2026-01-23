# Design Document: Dataset Import Script

## Overview

The Dataset Import Script is a simple utility that reads Excel files containing meal prep data (allergies, ingredients, recipes, and recipe-ingredient relationships) and populates the Entity Framework Core database. The script reads the four Excel files in the correct order to maintain referential integrity and outputs progress information as it runs.

The script can be implemented as either a console application or a database seeder method that runs during application startup. It uses EPPlus for Excel file reading and Entity Framework Core for database operations.

## Architecture

The script follows a simple procedural approach:

```
┌─────────────────────────────────────────┐
│      DatasetImporter                    │
│  (Console App or Seeder Method)         │
│                                          │
│  1. Import Allergies                    │
│  2. Import Ingredients                  │
│  3. Import Recipes                      │
│  4. Import RecipeIngredients            │
└────────┬────────────────────────────────┘
         │
         ├──────────────┬─────────────────┐
         ▼              ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ EPPlus       │  │ EF Core      │  │ Console      │
│ (Read Excel) │  │ (Save Data)  │  │ (Output)     │
└──────────────┘  └──────────────┘  └──────────────┘
         │              │
         ▼              ▼
┌──────────────┐  ┌──────────────┐
│ Excel Files  │  │  Database    │
│ (files/ dir) │  │  (SQL)       │
└──────────────┘  └──────────────┘
```

**Key Design Decisions:**

1. **Simple Procedural Flow**: No complex service architecture - just read and save
2. **Sequential Processing**: Import files one at a time in the correct order
3. **Direct Entity Creation**: Map Excel rows directly to EF Core entities
4. **Console Output**: Simple progress messages to console/log
5. **Single Transaction**: Wrap entire import in one transaction for simplicity

## Components and Interfaces

### DatasetImporter Class

The main class that orchestrates the import process.

**Implementation:**
```csharp
public class DatasetImporter
{
    private readonly ApplicationDbContext _context;
    private readonly string _filesDirectory;
    
    public DatasetImporter(ApplicationDbContext context, string filesDirectory)
    {
        _context = context;
        _filesDirectory = filesDirectory;
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
    
    private async Task ImportAllergiesAsync() { /* ... */ }
    private async Task ImportIngredientsAsync() { /* ... */ }
    private async Task ImportRecipesAsync() { /* ... */ }
    private async Task ImportRecipeIngredientsAsync() { /* ... */ }
}
```

**Key Methods:**

**ImportAllergiesAsync():**
- Opens Dataset_Allergy.xlsx using EPPlus
- Reads first worksheet
- Loops through rows starting from row 2
- Creates Allergy entities from AllergyName column (column A)
- Adds to DbContext
- Saves changes
- Outputs count

**ImportIngredientsAsync():**
- Opens Dataset_Ingredient.xlsx using EPPlus
- Reads first worksheet
- Loops through rows starting from row 2
- Creates Ingredient entities from columns A-D:
  - Column A: IngredientName
  - Column B: Unit
  - Column C: CaloPerUnit
  - Column D: IsAllergen
- Adds to DbContext
- Saves changes
- Outputs count

**ImportRecipesAsync():**
- Opens Dataset_Recipe.xlsx using EPPlus
- Reads first worksheet
- Loops through rows starting from row 2
- Creates Recipe entities from columns A-F:
  - Column A: RecipeName
  - Column B: Instructions
  - Column C: TotalCalories
  - Column D: ProteinG
  - Column E: FatG
  - Column F: CarbsG
- Adds to DbContext
- Saves changes
- Outputs count

**ImportRecipeIngredientsAsync():**
- Opens Dataset_Recipe_Ingredient.xlsx using EPPlus
- Reads first worksheet
- Loops through rows starting from row 2
- Creates RecipeIngredient entities from columns A-C:
  - Column A: RecipeId
  - Column B: IngredientId
  - Column C: Amount
- Adds to DbContext
- Saves changes
- Outputs count

### Helper Methods

**ReadExcelFile():**
```csharp
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
```

**GetCellValue():**
```csharp
private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
{
    return worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
}

private decimal GetDecimalValue(ExcelWorksheet worksheet, int row, int col)
{
    var value = worksheet.Cells[row, col].Value;
    return value != null ? Convert.ToDecimal(value) : 0m;
}

private bool GetBoolValue(ExcelWorksheet worksheet, int row, int col)
{
    var value = worksheet.Cells[row, col].Value?.ToString();
    return value == "1" || value?.ToLower() == "true";
}
```

## Data Models

The service works with existing Entity Framework Core entities:

### Allergy Entity
```csharp
public class Allergy
{
    public int AllergyId { get; set; }
    public string AllergyName { get; set; }
}
```

**Excel Mapping (Dataset_Allergy.xlsx):**
- Column A → AllergyName

### Ingredient Entity
```csharp
public class Ingredient
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; }
    public string Unit { get; set; }
    public decimal CaloPerUnit { get; set; }
    public bool IsAllergen { get; set; }
}
```

**Excel Mapping (Dataset_Ingredient.xlsx):**
- Column A → IngredientName
- Column B → Unit
- Column C → CaloPerUnit
- Column D → IsAllergen

### Recipe Entity
```csharp
public class Recipe
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; }
    public string Instructions { get; set; }
    public decimal TotalCalories { get; set; }
    public decimal ProteinG { get; set; }
    public decimal FatG { get; set; }
    public decimal CarbsG { get; set; }
}
```

**Excel Mapping (Dataset_Recipe.xlsx):**
- Column A → RecipeName
- Column B → Instructions
- Column C → TotalCalories
- Column D → ProteinG
- Column E → FatG
- Column F → CarbsG

### RecipeIngredient Entity
```csharp
public class RecipeIngredient
{
    public int RecipeId { get; set; }
    public int IngredientId { get; set; }
    public decimal Amount { get; set; }
    
    public Recipe Recipe { get; set; }
    public Ingredient Ingredient { get; set; }
}
```

**Excel Mapping (Dataset_Recipe_Ingredient.xlsx):**
- Column A → RecipeId
- Column B → IngredientId
- Column C → Amount

## Import Process Flow

The import process follows this simple sequence:

```
1. Begin Transaction
   
2. Import Allergy Data
   ├─ Open Dataset_Allergy.xlsx
   ├─ Read first worksheet
   ├─ Loop rows 2 to end
   ├─ Create Allergy entity for each row
   ├─ Add to DbContext
   ├─ SaveChanges
   └─ Output count
   
3. Import Ingredient Data
   ├─ Open Dataset_Ingredient.xlsx
   ├─ Read first worksheet
   ├─ Loop rows 2 to end
   ├─ Create Ingredient entity for each row
   ├─ Add to DbContext
   ├─ SaveChanges
   └─ Output count
   
4. Import Recipe Data
   ├─ Open Dataset_Recipe.xlsx
   ├─ Read first worksheet
   ├─ Loop rows 2 to end
   ├─ Create Recipe entity for each row
   ├─ Add to DbContext
   ├─ SaveChanges
   └─ Output count
   
5. Import RecipeIngredient Data
   ├─ Open Dataset_Recipe_Ingredient.xlsx
   ├─ Read first worksheet
   ├─ Loop rows 2 to end
   ├─ Create RecipeIngredient entity for each row
   ├─ Add to DbContext
   ├─ SaveChanges
   └─ Output count
   
6. Commit Transaction
   └─ Output success message
```

**Error Handling:**
- If any file is missing or corrupted, throw exception and rollback transaction
- If any database error occurs, rollback transaction
- Simple all-or-nothing approach


## Correctness Properties

A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.

### Property 1: Complete Row Reading

*For any* Excel dataset file with N data rows (excluding header), the import script should read and process exactly N rows.

**Validates: Requirements 1.1, 2.1, 3.1, 4.1**

### Property 2: Valid Data Persistence

*For any* Excel dataset file containing valid entity data, importing the file should result in all entities being created in the database.

**Validates: Requirements 1.2, 2.2, 3.2, 4.2**

### Property 3: Import Count Output

*For any* completed dataset file import, the console output should display the count of imported records.

**Validates: Requirements 1.4, 2.4, 3.4, 4.4**

## Error Handling

The import script uses a simple all-or-nothing approach:

**Transaction Management:**
- Entire import wrapped in a single database transaction
- If any step fails, entire transaction is rolled back
- No partial imports - either all data is imported or none

**File Errors:**
- Missing file: Throw FileNotFoundException, rollback transaction
- Corrupted Excel file: Throw exception, rollback transaction
- Invalid worksheet: Throw exception, rollback transaction

**Data Errors:**
- Null or empty cells: Handle gracefully by using default values or empty strings
- Invalid data types: Let EF Core handle conversion, throw if incompatible

**Database Errors:**
- Connection failure: Throw exception, rollback transaction
- Constraint violation: Throw exception, rollback transaction
- Foreign key violation: Throw exception, rollback transaction

## Testing Strategy

The dataset import script requires testing to ensure it correctly reads Excel files and populates the database.

### Unit Testing Approach

Unit tests focus on verifying the import logic with known test data:

**Import Method Tests:**
- Test importing each dataset file individually
- Test importing all files in sequence
- Test with known test data and verify database contents
- Test transaction rollback on error

**Excel Reading Tests:**
- Test reading valid Excel files
- Test handling missing files
- Test reading from first worksheet
- Test skipping header row (row 1)
- Test reading multiple rows

**Data Mapping Tests:**
- Test mapping Excel columns to entity properties
- Test handling null/empty cells
- Test data type conversions (string to decimal, string to bool)

**Integration Tests:**
- Test full import with all four files
- Test import order (Allergy → Ingredient → Recipe → RecipeIngredient)
- Test foreign key relationships are maintained
- Test transaction commit on success
- Test transaction rollback on failure

### Property-Based Testing Approach

Property-based tests verify universal properties across randomized inputs using CsCheck or FSCheck:

**Configuration:**
- Minimum 100 iterations per property test
- Each test tagged with: **Feature: dataset-import-service, Property N: [property text]**

**Property Test Implementations:**

**Property 1: Complete Row Reading**
- Generate Excel file with N random data rows (plus header)
- Import file
- Verify exactly N entities were created in database
- Tag: **Feature: dataset-import-service, Property 1: Complete Row Reading**

**Property 2: Valid Data Persistence**
- Generate random valid entities (Allergy, Ingredient, Recipe)
- Write to Excel file
- Import file
- Verify all entities exist in database with correct data
- Tag: **Feature: dataset-import-service, Property 2: Valid Data Persistence**

**Property 3: Import Count Output**
- Generate random entities
- Capture console output during import
- Verify output contains the count of imported records
- Tag: **Feature: dataset-import-service, Property 3: Import Count Output**

### Testing Library Selection

For C# property-based testing, use **CsCheck** (recommended):
- Modern, actively maintained
- Better performance
- Native C# implementation
- Good integration with xUnit/NUnit

### Test Organization

```
Tests/
├── Unit/
│   ├── DatasetImporterTests.cs
│   ├── ExcelReadingTests.cs
│   ├── DataMappingTests.cs
│   └── TransactionTests.cs
└── Properties/
    ├── RowReadingProperties.cs
    ├── DataPersistenceProperties.cs
    └── OutputProperties.cs
```

### Test Data Management

**For Unit Tests:**
- Use in-memory database (EF Core InMemory provider)
- Create small test Excel files in test resources folder
- Use known test data for verification

**For Property Tests:**
- Generate Excel files dynamically using EPPlus
- Use temporary directories for test files
- Clean up generated files after tests
- Use CsCheck generators for entity data
