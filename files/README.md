# Training Dataset Excel File Structure

## File: PRN222_Datasets.xlsx

This Excel file contains training datasets for the AI-powered meal recommendation system. The file is automatically imported when the application starts for the first time and the database is empty.

## Column Structure

The Excel file should have the following columns in order:

| Column | Name | Type | Required | Description | Example |
|--------|------|------|----------|-------------|---------|
| A | CustomerSegment | String | Yes | The customer segment or category | "Athletic", "Weight Loss", "Diabetic" |
| B | PreferredMealTypes | JSON Array | Yes | Array of preferred meal types | `["Breakfast","Lunch","Dinner"]` |
| C | AverageCalorieTarget | Integer | Yes | Target daily calorie intake | 2000 |
| D | CommonAllergies | JSON Array | No | Array of common allergies for this segment | `["Peanuts","Shellfish"]` or `[]` |
| E | RecommendationWeights | JSON Object | Yes | Scoring weights for recommendation algorithm | `{"protein":0.35,"carbs":0.40,"fats":0.25}` |

## Data Format Requirements

### CustomerSegment
- Must not be empty
- Maximum 100 characters
- Examples: "Athletic", "Weight Loss", "Diabetic", "Muscle Gain", "Vegetarian", "Vegan"

### PreferredMealTypes
- Must be valid JSON array format
- Can also accept comma-separated values (will be converted to JSON)
- Examples:
  - JSON: `["Breakfast","Lunch","Dinner","Snack"]`
  - CSV: `Breakfast, Lunch, Dinner` (will be converted to JSON)

### AverageCalorieTarget
- Must be a positive integer
- Range: 1 to 10000
- Typical values: 1600-3200

### CommonAllergies
- Must be valid JSON array format or empty
- Can also accept comma-separated values (will be converted to JSON)
- Use empty array `[]` if no common allergies
- Examples:
  - `["Peanuts","Shellfish","Gluten"]`
  - `[]` (no allergies)

### RecommendationWeights
- Must be valid JSON object format
- Contains scoring weights for the recommendation algorithm
- Typical keys:
  - `protein`: Weight for protein content matching (0.0-1.0)
  - `carbs`: Weight for carbohydrate content matching (0.0-1.0)
  - `fats`: Weight for fat content matching (0.0-1.0)
  - `variety`: Weight for meal variety bonus (0.0-1.0)
  - `calorie_alignment`: Weight for calorie goal alignment (0.0-1.0)
- Example: `{"protein":0.35,"carbs":0.40,"fats":0.25,"variety":0.20,"calorie_alignment":0.15}`

## Sample Data

The file includes 10 sample customer segments:

1. **Athletic** - High calorie, balanced macros, active lifestyle
2. **Weight Loss** - Lower calorie, higher protein
3. **Diabetic** - Controlled carbs, avoids sugar
4. **Muscle Gain** - Very high calorie, high protein
5. **Vegetarian** - No meat, includes eggs and dairy
6. **Vegan** - Plant-based only
7. **Heart Health** - Lower calorie, heart-healthy fats
8. **Keto** - Very low carb, high fat
9. **Balanced Diet** - Standard balanced nutrition
10. **Senior Health** - Moderate calorie, nutrient-dense

## Import Process

1. The system checks if the database is empty on startup
2. If empty, it automatically imports from `files/PRN222_Datasets.xlsx`
3. Each row is validated before import
4. Invalid rows are logged but don't stop the import process
5. Valid rows are imported into the TrainingDatasets table
6. An import summary is logged with success/error counts

## Validation Rules

- All required fields must be present
- JSON fields must be valid JSON format
- Calorie target must be positive (1-10000)
- Customer segment must not exceed 100 characters
- Invalid rows are skipped with error messages logged

## Manual Import

Administrators can also trigger manual imports through the Dataset Management interface in the admin panel.

## Generating the Sample File

To generate a new sample file, run the `GenerateSampleExcelFile` test in the test project (currently skipped by default).
