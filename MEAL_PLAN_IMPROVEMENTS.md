# Meal Plan Improvements

## Changes Made

### 1. Removed "Snack" Meal Type
- **File**: `src/MealPrepService.Web/PresentationLayer/ViewModels/MealPlanViewModel.cs`
- **Change**: Updated `MealTypeOptions` to only include: `Breakfast`, `Lunch`, `Dinner`
- **Impact**: Users can now only select these three meal types when adding meals to their plan

### 2. Added Meal Type Ordering
- **File**: `src/MealPrepService.Web/PresentationLayer/ViewModels/MealPlanViewModel.cs`
- **Change**: Added `GetMealTypeOrder()` helper method to define the correct order:
  - Breakfast = 1
  - Lunch = 2
  - Dinner = 3
- **Impact**: Meals are now displayed in the correct chronological order throughout the day

### 3. Fixed Meal Ordering at Database Layer (CRITICAL FIX)
- **File**: `src/MealPrepService.DataAccessLayer/Repositories/MealPlanRepository.cs`
- **Change**: Updated `GetWithMealsAndRecipesAsync()` to order meals by:
  1. First by `ServeDate` (chronological order)
  2. Then by meal type order (Breakfast → Lunch → Dinner)
- **Added**: `GetMealTypeOrder()` helper method in repository
- **Impact**: Meals are now ordered at the source (database layer), ensuring consistent ordering throughout the entire application

### 4. Updated Meal Display Order in View
- **File**: `src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Details.cshtml`
- **Change**: Modified the meal loop to use `OrderBy(m => MealViewModel.GetMealTypeOrder(m.MealType))`
- **Impact**: Additional ordering in the view as a safety measure

### 5. Fixed Meal Ordering in Controller
- **File**: `src/MealPrepService.Web/PresentationLayer/Controllers/MealPlanController.cs`
- **Change**: Updated `CalculateNutritionTotals()` method to sort meals by meal type order after grouping by date
- **Impact**: Ensures meals are properly ordered when calculating daily nutrition totals

### 6. Highlighted Today's Meals
- **File**: `src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Details.cshtml`
- **Changes**:
  - Added logic to detect if a day is today (`var isToday = day.Key.Date == today`)
  - Applied special styling to today's card:
    - Blue border (`border-primary border-3`)
    - Blue header background (`bg-primary text-white`)
    - Added "TODAY" badge with star icon
  - Enhanced shadow for today's card
- **Impact**: Today's meals are now visually prominent with a blue border, blue header, and a "TODAY" badge

## Visual Improvements

### Today's Meals Card
- **Border**: Thick blue border (3px)
- **Header**: Blue background with white text
- **Badge**: Yellow "TODAY" badge with star icon
- **Shadow**: Enhanced shadow for better visibility

### Regular Day Cards
- **Border**: Standard card border
- **Header**: Light gray background
- **Shadow**: Standard shadow

## Technical Details

### Ordering Strategy (Multi-Layer Approach)
The meal ordering is now enforced at **three levels** for maximum reliability:

1. **Database Layer** (Primary): `MealPlanRepository.GetWithMealsAndRecipesAsync()`
   - Orders meals immediately after loading from database
   - Ensures all data retrieved is pre-sorted
   - Most efficient approach

2. **Controller Layer** (Secondary): `MealPlanController.CalculateNutritionTotals()`
   - Re-sorts meals when calculating daily nutrition
   - Provides additional safety layer

3. **View Layer** (Tertiary): `Details.cshtml`
   - Final ordering in the view template
   - Last line of defense

This multi-layer approach ensures meals are **always** displayed in the correct order (Breakfast → Lunch → Dinner) regardless of:
- Database insertion order
- Concurrent updates
- Caching issues
- Any other potential ordering disruptions

## User Experience Benefits

1. **Clearer Meal Structure**: Only showing Breakfast, Lunch, and Dinner makes the meal plan more straightforward
2. **Consistent Ordering**: Meals are **always** displayed in chronological order (morning to evening) at all layers
3. **Easy Navigation**: Today's meals are immediately visible with prominent highlighting
4. **Better Planning**: Users can quickly see what they need to prepare today vs. future days
5. **Predictable Display**: Meals appear in the same order every time, regardless of when they were added

## Testing Recommendations

1. Create a new meal plan spanning multiple days
2. Add meals in **random order** (e.g., Dinner first, then Breakfast, then Lunch)
3. Verify meals appear in correct order within each day (Breakfast → Lunch → Dinner)
4. Refresh the page multiple times to ensure ordering is consistent
5. Check that today's date is highlighted with blue border and "TODAY" badge
6. Verify the "Add Meal" dropdown only shows Breakfast, Lunch, and Dinner options
7. Test with AI-generated meal plans to ensure ordering works there too
8. Add meals on different days and verify cross-day ordering is correct
