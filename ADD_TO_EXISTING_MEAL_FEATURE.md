# Add to Existing Meal Feature

## Overview
Modified the "Add Meal" functionality to intelligently check if a meal of the same type already exists for the selected date. If it does, the new recipes are added to the existing meal instead of creating a duplicate meal entry.

## Problem Solved
Previously, if a user added a "Breakfast" meal for Monday, and then tried to add another "Breakfast" for the same Monday, the system would create two separate "Breakfast" meals. This resulted in:
- Duplicate meal entries
- Confusing meal plan display
- Incorrect nutrition calculations

## Solution
The system now:
1. Checks if a meal of the same type exists for the selected date
2. If found, adds the new recipes to the existing meal
3. If not found, creates a new meal
4. Prevents duplicate recipes within the same meal

## Changes Made

### Business Logic Layer

**File**: `src/MealPrepService.BusinessLogicLayer/Services/MealPlanService.cs`

**Method**: `AddMealToPlanAsync(Guid planId, MealDto mealDto)`

**Key Changes**:

1. **Check for Existing Meal**:
   ```csharp
   var existingMeal = (await _unitOfWork.Meals.GetAllAsync())
       .FirstOrDefault(m => 
           m.PlanId == planId && 
           m.MealType.ToLower() == mealDto.MealType.ToLower() && 
           m.ServeDate.Date == mealDto.ServeDate.Date);
   ```

2. **Reuse or Create**:
   - If existing meal found: Use it
   - If not found: Create new meal

3. **Prevent Duplicate Recipes**:
   ```csharp
   var existingRecipeIds = _unitOfWork.MealRecipes
       .Where(mr => mr.MealId == meal.Id)
       .Select(mr => mr.RecipeId)
       .ToHashSet();
   ```
   - Checks if recipe already exists in the meal
   - Skips duplicate recipes
   - Logs how many recipes were added vs skipped

4. **Enhanced Logging**:
   - Logs when existing meal is found
   - Logs when new meal is created
   - Logs recipe addition counts
   - Logs duplicate recipe skips

## User Experience

### Before
1. User adds Breakfast with Recipe A on Monday
2. User adds Breakfast with Recipe B on Monday
3. Result: Two separate Breakfast meals on Monday

### After
1. User adds Breakfast with Recipe A on Monday
2. User adds Breakfast with Recipe B on Monday
3. Result: One Breakfast meal on Monday with both Recipe A and Recipe B

## Benefits

1. **No Duplicates**: Only one meal per type per day
2. **Cleaner Display**: Meal plan shows organized meals
3. **Accurate Nutrition**: Correct totals without duplication
4. **Better UX**: Users can incrementally build meals
5. **Flexible**: Can add recipes one at a time or all at once

## Technical Details

### Matching Logic
Meals are considered the same if they match:
- Same `PlanId`
- Same `MealType` (case-insensitive)
- Same `ServeDate` (date only, ignoring time)

### Recipe Deduplication
- Checks existing `MealRecipe` entries
- Skips recipes already linked to the meal
- Prevents duplicate recipe-meal relationships

### Database Operations
- Efficient: Only queries meals for the specific plan
- Safe: Uses existing meal ID for recipe links
- Atomic: All changes saved in single transaction

## Example Scenarios

### Scenario 1: Building a Meal Incrementally
1. Monday: Add Breakfast with "Scrambled Eggs"
2. Later: Add Breakfast with "Toast" on same Monday
3. Result: One Breakfast with both recipes

### Scenario 2: Avoiding Duplicates
1. Add Breakfast with "Oatmeal" on Tuesday
2. Try to add Breakfast with "Oatmeal" again on Tuesday
3. Result: Recipe skipped (already exists), no duplicate

### Scenario 3: Different Meal Types
1. Add Breakfast on Wednesday
2. Add Lunch on Wednesday
3. Result: Two separate meals (different types)

### Scenario 4: Different Dates
1. Add Breakfast on Thursday
2. Add Breakfast on Friday
3. Result: Two separate meals (different dates)

## Logging Examples

### New Meal Created
```
Creating new breakfast meal for 2026-01-27
Added 2 recipe(s) to meal, skipped 0 duplicate(s)
New meal added to plan {PlanId}: breakfast on 2026-01-27
```

### Adding to Existing Meal
```
Found existing breakfast meal for 2026-01-27, adding recipes to it
Added 1 recipe(s) to meal, skipped 0 duplicate(s)
Recipes added to existing meal in plan {PlanId}: breakfast on 2026-01-27
```

### Skipping Duplicates
```
Recipe {RecipeId} already exists in meal, skipping
Added 1 recipe(s) to meal, skipped 1 duplicate(s)
```

## Testing Recommendations

1. **Basic Addition**:
   - Add meal with recipes
   - Verify meal appears in plan

2. **Add to Existing**:
   - Add Breakfast with Recipe A
   - Add Breakfast with Recipe B (same date)
   - Verify only one Breakfast with both recipes

3. **Duplicate Prevention**:
   - Add Breakfast with Recipe A
   - Try adding Breakfast with Recipe A again
   - Verify recipe not duplicated

4. **Different Types**:
   - Add Breakfast, Lunch, Dinner on same day
   - Verify three separate meals

5. **Different Dates**:
   - Add Breakfast on multiple days
   - Verify separate meals per day

6. **Mixed Scenarios**:
   - Add multiple recipes at once
   - Add single recipes incrementally
   - Verify all work correctly

## Future Enhancements

1. **UI Indication**: Show when adding to existing meal
2. **Recipe Management**: Allow removing recipes from meals
3. **Meal Editing**: Edit existing meal recipes
4. **Bulk Operations**: Add same recipe to multiple days
5. **Templates**: Save meal combinations as templates
