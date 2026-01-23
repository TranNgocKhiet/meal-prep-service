# Requirements Document

## Introduction

This document specifies the requirements for a one-time data import script that reads Excel files containing allergy, ingredient, recipe, and recipe-ingredient data and populates the meal prep service database. The script should be simple, handle basic validation, and maintain referential integrity during import.

## Glossary

- **Import_Script**: A simple utility that reads Excel files and imports data into the database
- **Dataset_File**: An Excel file containing structured data for import (Allergy, Ingredient, Recipe, or RecipeIngredient)
- **Database**: The Entity Framework Core database storing meal prep service data
- **EPPlus**: The library used for reading Excel files

## Requirements

### Requirement 1: Import Allergy Data

**User Story:** As a developer, I want to import allergy data from an Excel file, so that the database is populated with allergen information.

#### Acceptance Criteria

1. WHEN the Import_Script processes Dataset_Allergy.xlsx, THE Import_Script SHALL read all rows containing AllergyName values
2. WHEN a row contains a non-empty AllergyName, THE Import_Script SHALL create an Allergy entity in the Database
3. WHEN reading rows, THE Import_Script SHALL skip the header row and begin reading from row 2
4. WHEN the allergy import completes, THE Import_Script SHALL output the count of imported records

### Requirement 2: Import Ingredient Data

**User Story:** As a developer, I want to import ingredient data from an Excel file, so that the database has a complete ingredient list.

#### Acceptance Criteria

1. WHEN the Import_Script processes Dataset_Ingredient.xlsx, THE Import_Script SHALL read all rows containing IngredientName, Unit, CaloPerUnit, and IsAllergen values
2. WHEN a row contains valid ingredient data, THE Import_Script SHALL create an Ingredient entity in the Database
3. WHEN reading rows, THE Import_Script SHALL skip the header row and begin reading from row 2
4. WHEN the ingredient import completes, THE Import_Script SHALL output the count of imported records

### Requirement 3: Import Recipe Data

**User Story:** As a developer, I want to import recipe data from an Excel file, so that the database contains all available recipes.

#### Acceptance Criteria

1. WHEN the Import_Script processes Dataset_Recipe.xlsx, THE Import_Script SHALL read all rows containing RecipeName, Instructions, TotalCalories, ProteinG, FatG, and CarbsG values
2. WHEN a row contains valid recipe data, THE Import_Script SHALL create a Recipe entity in the Database
3. WHEN reading rows, THE Import_Script SHALL skip the header row and begin reading from row 2
4. WHEN the recipe import completes, THE Import_Script SHALL output the count of imported records

### Requirement 4: Import Recipe-Ingredient Relationships

**User Story:** As a developer, I want to import recipe-ingredient relationships from an Excel file, so that each recipe has its ingredient list.

#### Acceptance Criteria

1. WHEN the Import_Script processes Dataset_Recipe_Ingredient.xlsx, THE Import_Script SHALL read all rows containing RecipeId, IngredientId, and Amount values
2. WHEN a row contains valid relationship data, THE Import_Script SHALL create a RecipeIngredient entity in the Database
3. WHEN reading rows, THE Import_Script SHALL skip the header row and begin reading from row 2
4. WHEN the recipe-ingredient import completes, THE Import_Script SHALL output the count of imported records

### Requirement 5: Maintain Import Order

**User Story:** As a developer, I want the import script to respect foreign key relationships, so that data integrity is maintained.

#### Acceptance Criteria

1. WHEN the Import_Script executes, THE Import_Script SHALL import Dataset_Allergy.xlsx first
2. WHEN allergy import completes, THE Import_Script SHALL import Dataset_Ingredient.xlsx second
3. WHEN ingredient import completes, THE Import_Script SHALL import Dataset_Recipe.xlsx third
4. WHEN recipe import completes, THE Import_Script SHALL import Dataset_Recipe_Ingredient.xlsx fourth

### Requirement 6: Read Excel Files

**User Story:** As a developer, I want the import script to read Excel files using EPPlus, so that data can be extracted from the dataset files.

#### Acceptance Criteria

1. WHEN the Import_Script opens a Dataset_File, THE Import_Script SHALL use EPPlus library to read the Excel workbook
2. WHEN reading a Dataset_File, THE Import_Script SHALL read from the first worksheet in the workbook
3. WHEN reading rows, THE Import_Script SHALL skip the header row and begin reading from row 2
4. WHEN reading cell values, THE Import_Script SHALL handle null or empty cells appropriately
