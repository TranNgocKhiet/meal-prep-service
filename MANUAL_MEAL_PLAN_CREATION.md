# Manual Meal Plan Creation with Ingredient Customization

## Overview
The manual meal plan creation functionality allows customers to create custom meal plans by selecting recipes from the database and customizing ingredient amounts for each recipe.

## Features

### 1. Manual Meal Plan Creation
- Customers can create meal plans without AI assistance
- Set custom plan name, start date, and end date
- Choose between AI generation or manual creation

### 2. Recipe Selection with Ingredients
- Browse all available recipes with nutrition information
- View ingredients and amounts for each recipe
- Select multiple recipes for a single meal

### 3. Ingredient Amount Customization
- Customize the amount of each ingredient in selected recipes
- Original amounts are preserved and shown for reference
- Modified amounts are highlighted in the UI
- Support for different units (grams, cups, tablespoons, etc.)

### 4. Meal Type and Date Management
- Choose meal type: Breakfast, Lunch, Dinner, or Snack
- Set serve date within the meal plan date range
- Validation ensures dates are within plan boundaries

## User Workflow

### Step 1: Create Meal Plan
1. Navigate to **MealPlan → Create**
2. Enter plan name, start date, and end date
3. Leave "Generate with AI" unchecked for manual creation
4. Click "Create Meal Plan"

### Step 2: Add Meals to Plan
1. From the meal plan details page, click "Add Meal"
2. Select meal type (Breakfast, Lunch, Dinner, Snack)
3. Choose serve date within plan range
4. Browse and select recipes

### Step 3: Customize Ingredients (Optional)
1. When a recipe is selected, its ingredients section appears
2. Modify ingredient amounts as needed
3. Original amounts are preserved for reference
4. Modified amounts are highlighted

### Step 4: Complete Meal Addition
1. Review selected recipes and customizations
2. Click "Add [X] Recipe(s) to Plan"
3. Return to meal plan details to see the added meal

## Technical Implementation

### Enhanced Services
- **RecipeService**: Added `GetAllWithIngredientsAsync()` method
- **MealPlanService**: Added `AddMealToPlanWithCustomIngredientsAsync()` method
- **RecipeRepository**: Added `GetAllWithIngredientsAsync()` method

### New View Models
- **RecipeIngredientCustomizationViewModel**: Handles ingredient amount customization
- Enhanced **AddMealToPlanViewModel**: Supports ingredient customizations
- Enhanced **RecipeSelectionViewModel**: Includes ingredient information

### Database Integration
- Utilizes existing Recipe → RecipeIngredient → Ingredient relationships
- Ingredient customizations are logged for tracking
- Future enhancement: Store customizations in dedicated table

### UI Features
- Interactive recipe cards with selection checkboxes
- Dynamic ingredient sections that appear when recipes are selected
- Real-time validation and visual feedback
- Responsive design for mobile and desktop

## Navigation Paths

### From Meal Plan List
```
Meal Plans → Create → Manual Creation → Add Meals → Select Recipes & Customize Ingredients
```

### From Meal Plan Details
```
Meal Plan Details → Add Meal → Select Recipes & Customize Ingredients
```

## Validation Rules

### Meal Plan Creation
- Plan name: Required, max 100 characters
- Start date: Cannot be in the past
- End date: Must be after start date
- Duration: Max 30 days (view model), max 90 days (service)

### Meal Addition
- Meal type: Required (Breakfast, Lunch, Dinner, Snack)
- Serve date: Required, must be within plan date range
- Recipes: At least one recipe must be selected
- Ingredient amounts: Must be positive numbers

## Future Enhancements

### Planned Features
1. **Custom Recipe Variants**: Store ingredient customizations as recipe variants
2. **Nutrition Recalculation**: Automatically recalculate nutrition based on custom amounts
3. **Shopping List Integration**: Generate shopping lists with custom amounts
4. **Meal Templates**: Save frequently used meal combinations
5. **Portion Scaling**: Scale entire recipes up or down by percentage

### Database Schema Extensions
```sql
-- Future table for storing ingredient customizations
CREATE TABLE MealRecipeIngredientCustomizations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    MealRecipeId UNIQUEIDENTIFIER FOREIGN KEY,
    IngredientId UNIQUEIDENTIFIER FOREIGN KEY,
    CustomAmount FLOAT NOT NULL,
    OriginalAmount FLOAT NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
```

## Benefits

### For Customers
- Full control over meal planning
- Ability to adjust recipes to personal preferences
- Flexibility to accommodate dietary restrictions
- Visual feedback for ingredient modifications

### For Business
- Increased user engagement with manual planning
- Data collection on ingredient preferences
- Foundation for advanced meal customization features
- Reduced dependency on AI for basic meal planning

## Integration Points

### Existing Features
- Works seamlessly with AI-generated meal plans
- Integrates with existing nutrition calculation system
- Compatible with meal plan details and viewing functionality
- Supports existing user authentication and authorization

### Related Systems
- **Recipe Management**: Uses existing recipe database
- **Ingredient Management**: Leverages ingredient catalog
- **Nutrition Tracking**: Displays nutrition information
- **User Profiles**: Respects user permissions and ownership

This manual meal plan creation system provides customers with complete flexibility while maintaining the structured approach of the existing meal planning system.