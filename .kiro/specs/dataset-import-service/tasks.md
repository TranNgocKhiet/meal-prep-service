# Implementation Plan: Dataset Import Script

## Overview

This plan implements a simple dataset import script that reads four Excel files and populates the meal prep service database. The script will be implemented as a console application or database seeder method that can be run once to import the initial data.

## Tasks

- [x] 1. Set up DatasetImporter class structure
  - Create DatasetImporter class with constructor accepting DbContext and files directory path
  - Add ImportAllAsync method as main entry point
  - Add private methods for each dataset type (ImportAllergiesAsync, ImportIngredientsAsync, ImportRecipesAsync, ImportRecipeIngredientsAsync)
  - Add helper methods for Excel file operations (OpenExcelFile, GetCellValue, GetDecimalValue, GetBoolValue)
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 6.1, 6.2_

- [ ] 2. Implement allergy data import
  - [x] 2.1 Implement ImportAllergiesAsync method
    - Open Dataset_Allergy.xlsx using EPPlus
    - Read first worksheet
    - Loop through rows starting from row 2
    - Read AllergyName from column A
    - Create Allergy entities for non-empty values
    - Add entities to DbContext
    - Save changes
    - Output count to console
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  
  - [ ]* 2.2 Write property test for allergy import
    - **Property 1: Complete Row Reading**
    - **Validates: Requirements 1.1**
  
  - [ ]* 2.3 Write unit tests for allergy import
    - Test reading valid allergy data
    - Test skipping header row
    - Test handling empty cells
    - _Requirements: 1.1, 1.2, 1.3_

- [ ] 3. Implement ingredient data import
  - [x] 3.1 Implement ImportIngredientsAsync method
    - Open Dataset_Ingredient.xlsx using EPPlus
    - Read first worksheet
    - Loop through rows starting from row 2
    - Read IngredientName (column A), Unit (column B), CaloPerUnit (column C), IsAllergen (column D)
    - Create Ingredient entities
    - Add entities to DbContext
    - Save changes
    - Output count to console
    - _Requirements: 2.1, 2.2, 2.3, 2.4_
  
  - [ ]* 3.2 Write property test for ingredient import
    - **Property 2: Valid Data Persistence**
    - **Validates: Requirements 2.2**
  
  - [ ]* 3.3 Write unit tests for ingredient import
    - Test reading valid ingredient data
    - Test data type conversions (decimal, bool)
    - Test handling null/empty cells
    - _Requirements: 2.1, 2.2, 6.4_

- [ ] 4. Implement recipe data import
  - [x] 4.1 Implement ImportRecipesAsync method
    - Open Dataset_Recipe.xlsx using EPPlus
    - Read first worksheet
    - Loop through rows starting from row 2
    - Read RecipeName (A), Instructions (B), TotalCalories (C), ProteinG (D), FatG (E), CarbsG (F)
    - Create Recipe entities
    - Add entities to DbContext
    - Save changes
    - Output count to console
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
  
  - [ ]* 4.2 Write unit tests for recipe import
    - Test reading valid recipe data
    - Test handling all nutritional fields
    - Test data type conversions
    - _Requirements: 3.1, 3.2_

- [ ] 5. Implement recipe-ingredient relationship import
  - [x] 5.1 Implement ImportRecipeIngredientsAsync method
    - Open Dataset_Recipe_Ingredient.xlsx using EPPlus
    - Read first worksheet
    - Loop through rows starting from row 2
    - Read RecipeId (column A), IngredientId (column B), Amount (column C)
    - Create RecipeIngredient entities
    - Add entities to DbContext
    - Save changes
    - Output count to console
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  
  - [ ]* 5.2 Write property test for output count
    - **Property 3: Import Count Output**
    - **Validates: Requirements 1.4, 2.4, 3.4, 4.4**
  
  - [ ]* 5.3 Write unit tests for recipe-ingredient import
    - Test reading valid relationship data
    - Test foreign key relationships
    - _Requirements: 4.1, 4.2_

- [ ] 6. Implement transaction management and main orchestration
  - [x] 6.1 Complete ImportAllAsync method
    - Begin database transaction
    - Call ImportAllergiesAsync
    - Call ImportIngredientsAsync
    - Call ImportRecipesAsync
    - Call ImportRecipeIngredientsAsync
    - Commit transaction on success
    - Rollback transaction on error
    - Output success/failure message
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  
  - [ ]* 6.2 Write unit tests for transaction management
    - Test transaction commit on success
    - Test transaction rollback on error
    - Test import order is maintained
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 7. Create console application or seeder integration
  - [x] 7.1 Create console application entry point OR integrate with DbSeeder
    - If console app: Create Program.cs with Main method
    - If seeder: Add method to existing DbSeeder class
    - Initialize DbContext
    - Create DatasetImporter instance
    - Call ImportAllAsync
    - Handle exceptions and output results
    - _Requirements: All_
  
  - [ ]* 7.2 Write integration tests
    - Test full import with all four files
    - Test with in-memory database
    - Verify all entities are created correctly
    - Verify foreign key relationships
    - _Requirements: All_

- [x] 8. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster implementation
- The script uses EPPlus library which should already be available in the project
- Excel files are expected to be in the `files/` directory
- Each import method outputs the count of imported records to the console
- The entire import runs in a single transaction for simplicity
- If any step fails, the entire import is rolled back
