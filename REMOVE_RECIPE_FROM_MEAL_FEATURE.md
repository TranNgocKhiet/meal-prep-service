# Remove Recipe from Meal Feature

## Overview
This feature allows users to remove individual recipes from meals in their meal plans. When a recipe is removed, the system intelligently handles two scenarios:
1. If the meal has multiple recipes, only the selected recipe is removed
2. If the meal has only one recipe, the entire meal is deleted

## Implementation Details

### 1. Service Layer (`MealPlanService.cs`)

**Method:** `RemoveRecipeFromMealAsync(Guid mealId, Guid recipeId, Guid accountId)`

**Logic:**
- Validates that the meal exists
- Verifies the user owns the meal plan (authorization check)
- Finds the MealRecipe junction record
- Counts remaining recipes in the meal
- If last recipe: Deletes the entire meal
- If multiple recipes: Removes only the specified recipe
- Saves changes to database

**Authorization:**
- Only the meal plan owner can remove recipes
- Managers cannot remove recipes from other users' plans (follows ownership model)

### 2. Controller Layer (`MealPlanController.cs`)

**Action:** `RemoveRecipeFromMeal(Guid mealId, Guid recipeId, Guid planId)`

**Features:**
- POST action with anti-forgery token validation
- Requires authentication (Customer or Manager role)
- Returns to meal plan details page after removal
- Displays success/error messages via TempData
- Handles exceptions gracefully

### 3. View Layer (`Details.cshtml`)

**UI Components:**
- Remove button placed next to Info button for each recipe
- Red/danger styling to indicate destructive action
- Trash icon for visual clarity
- Inline form with hidden fields for mealId, recipeId, and planId
- Anti-forgery token for security

**User Experience:**
- Confirmation dialog before removal
- Special warning if removing the last recipe (meal will be deleted)
- Responsive button layout using flexbox
- Consistent with existing UI patterns

## User Flow

1. User views meal plan details
2. User clicks "Remove" button next to a recipe
3. Confirmation dialog appears:
   - Standard: "Are you sure you want to remove this recipe from the meal?"
   - Last recipe: "Are you sure you want to remove this recipe from the meal? This will delete the entire meal as it's the last recipe."
4. User confirms
5. System processes removal
6. User redirected back to meal plan details
7. Success message displayed

## Edge Cases Handled

### Last Recipe in Meal
When removing the last recipe from a meal:
- The entire meal is deleted from the database
- User is warned in the confirmation dialog
- Meal disappears from the daily breakdown
- Daily nutrition totals are recalculated

### Authorization
- Users can only remove recipes from their own meal plans
- Attempting to remove from another user's plan results in authorization error
- Error message displayed and user redirected safely

### Non-existent Records
- If meal doesn't exist: NotFoundException thrown
- If recipe not in meal: NotFoundException thrown
- User-friendly error messages displayed

## Database Operations

### When Removing Recipe (Multiple Recipes in Meal)
```sql
DELETE FROM MealRecipes 
WHERE MealId = @mealId AND RecipeId = @recipeId
```

### When Removing Last Recipe
```sql
-- First deletes all MealRecipes (cascade)
DELETE FROM Meals WHERE Id = @mealId
```

## Security Considerations

1. **Anti-Forgery Token**: Protects against CSRF attacks
2. **Authorization Check**: Verifies user owns the meal plan
3. **Role-Based Access**: Only Customer and Manager roles can access
4. **Input Validation**: All GUIDs validated before processing
5. **Exception Handling**: Prevents information leakage through error messages

## Testing Recommendations

### Manual Testing Scenarios
1. Remove recipe from meal with multiple recipes
2. Remove last recipe from meal (should delete meal)
3. Try to remove recipe from another user's meal plan
4. Remove recipe and verify nutrition totals update
5. Remove recipe and verify daily breakdown updates
6. Cancel confirmation dialog (should not remove)

### Edge Cases to Test
1. Concurrent removal attempts
2. Removing already-removed recipe
3. Removing from non-existent meal
4. Authorization with different user roles

## Future Enhancements

Potential improvements for future iterations:
1. Undo functionality (restore removed recipe)
2. Bulk remove (remove multiple recipes at once)
3. Move recipe to different meal instead of removing
4. Archive removed recipes for history tracking
5. AJAX-based removal without page reload
6. Animation when recipe is removed

## Related Features

- **Add Meal to Plan**: Adds recipes to meals
- **Add to Existing Meal**: Adds recipes to existing meals without duplicating
- **Delete Meal Plan**: Deletes entire meal plan with all meals
- **Active Meal Plan**: Marks plan as active for grocery list generation

## Files Modified

1. `src/MealPrepService.BusinessLogicLayer/Interfaces/IMealPlanService.cs`
   - Added method signature

2. `src/MealPrepService.BusinessLogicLayer/Services/MealPlanService.cs`
   - Implemented RemoveRecipeFromMealAsync method

3. `src/MealPrepService.Web/PresentationLayer/Controllers/MealPlanController.cs`
   - Added RemoveRecipeFromMeal action

4. `src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Details.cshtml`
   - Added Remove button with form and confirmation

## Notes

- The feature maintains data integrity by handling the last-recipe scenario
- User experience is prioritized with clear warnings and confirmations
- Authorization is enforced at both service and controller layers
- The implementation follows existing patterns in the codebase
- No database migration required (uses existing tables and relationships)
