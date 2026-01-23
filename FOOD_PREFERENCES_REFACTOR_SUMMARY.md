# Food Preferences Refactor - Summary

## Status: ✅ COMPLETED

## Summary
Successfully refactored food preferences from a table-based many-to-many relationship to a simple text field. The AI now reads from four fields (Dietary Restrictions, Health Notes, Daily Calorie Goal, and Food Preferences) when generating meal plans.

## Changes Completed

### 1. Database Layer ✅
**File:** `src/MealPrepService.DataAccessLayer/Entities/HealthProfile.cs`
- Removed `ICollection<FoodPreference> FoodPreferences` navigation property
- Added `string? FoodPreferences` text field for free-form input

**File:** `src/MealPrepService.DataAccessLayer/Data/MealPrepDbContext.cs`
- Removed many-to-many relationship configuration for FoodPreferences

### 2. DTO Layer ✅
**File:** `src/MealPrepService.BusinessLogicLayer/DTOs/HealthProfileDto.cs`
- Changed `List<string> FoodPreferences` to `string? FoodPreferences`

### 3. Service Layer ✅
**File:** `src/MealPrepService.BusinessLogicLayer/Interfaces/IHealthProfileService.cs`
- Removed `AddFoodPreferenceAsync()` method
- Removed `RemoveFoodPreferenceAsync()` method

**File:** `src/MealPrepService.BusinessLogicLayer/Services/HealthProfileService.cs`
- Updated `CreateOrUpdateAsync()` to handle FoodPreferences text field
- Removed `AddFoodPreferenceAsync()` method implementation
- Removed `RemoveFoodPreferenceAsync()` method implementation
- Updated `GetByAccountIdAsync()` to not load FoodPreferences navigation
- Updated `MapToDtoAsync()` to map FoodPreferences as string

**File:** `src/MealPrepService.BusinessLogicLayer/Services/CustomerProfileAnalyzer.cs`
- Removed code that populated `context.Preferences` list
- Updated profile completeness check to not require preferences

**File:** `src/MealPrepService.BusinessLogicLayer/Services/OpenAIRecommendationService.cs`
- Updated AI prompt building to include `FoodPreferences` text field
- Removed code that processed `context.Preferences` list
- AI now reads from: Dietary Restrictions, Health Notes, Daily Calorie Goal, and Food Preferences

### 4. ViewModel Layer ✅
**File:** `src/MealPrepService.Web/PresentationLayer/ViewModels/HealthProfileViewModel.cs`
- Added `string? FoodPreferences` property with validation
- Removed `List<Guid> SelectedFoodPreferenceIds` property
- Removed `List<FoodPreferenceViewModel> AvailableFoodPreferences` property
- Removed `List<string> CurrentFoodPreferences` property
- Removed `FoodPreferenceViewModel` class
- Removed `AddFoodPreferenceViewModel` class

### 5. View Layer ✅
**File:** `src/MealPrepService.Web/PresentationLayer/Views/HealthProfile/Edit.cshtml`
- Added Food Preferences textarea (similar to Dietary Restrictions)
- Removed entire "Manage Food Preferences" section
- Removed add/remove food preference buttons
- Updated layout to show all text fields together

**File:** `src/MealPrepService.Web/PresentationLayer/Views/HealthProfile/Index.cshtml`
- Updated to display FoodPreferences as text instead of badges

**File:** `src/MealPrepService.Web/PresentationLayer/Views/HealthProfile/Create.cshtml`
- Removed food preferences selection section

### 6. Controller Layer ✅
**File:** `src/MealPrepService.Web/PresentationLayer/Controllers/HealthProfileController.cs`
- Removed `AddFoodPreference()` action
- Removed `RemoveFoodPreference()` action
- Removed `GetAvailableFoodPreferencesAsync()` helper method
- Removed `AddSelectedFoodPreferencesAsync()` helper method
- Updated `MapToViewModel()` to handle FoodPreferences as string
- Updated all references to remove `AvailableFoodPreferences`

### 7. Database Migration ✅
**File:** `src/MealPrepService.DataAccessLayer/Migrations/20260123121158_AddFoodPreferencesTextField.cs`
- Drops `HealthProfileFoodPreferences` junction table
- Adds `FoodPreferences` text column to HealthProfiles table
- Migration ready to apply with `dotnet ef database update`

## Build Status
✅ **All Projects Build Successfully**
- DataAccessLayer: No errors
- BusinessLogicLayer: No errors  
- Web: No errors

## How to Apply Changes

1. **Apply the database migration:**
   ```bash
   dotnet ef database update --project src/MealPrepService.DataAccessLayer --startup-project src/MealPrepService.Web
   ```

2. **Test the changes:**
   - Create/edit health profile with food preferences text
   - Generate AI meal plan and verify it uses the new field
   - Check that AI prompt includes all four fields

## Next Steps for Testing

1. ✅ Build completed successfully
2. ⏳ Apply database migration
3. ⏳ Test health profile creation
4. ⏳ Test health profile editing
5. ⏳ Test AI meal plan generation
6. ⏳ Verify AI uses all fields (Dietary Restrictions, Health Notes, Calorie Goal, Food Preferences)

## Notes

- Existing health profiles will have NULL in FoodPreferences field (users can fill it in when editing)
- The FoodPreference table still exists but is no longer used (can be removed in future cleanup)
- The change is backward compatible - existing profiles work fine with empty preferences

## Database Schema Changes

### Before
```
HealthProfiles table:
- No FoodPreferences column

HealthProfileFoodPreferences table (junction):
- HealthProfileId
- FoodPreferenceId

FoodPreferences table:
- Id
- PreferenceName
- IngredientId (optional)
```

### After
```
HealthProfiles table:
- FoodPreferences (string, nullable) - NEW

HealthProfileFoodPreferences table - DROPPED

FoodPreferences table - Can be kept or removed (not used anymore)
```

## AI Prompt Changes

### Before
AI received:
- Dietary Restrictions
- Health Notes
- Calorie Goal
- Preferred Ingredients (from FoodPreference table)

### After
AI receives:
- Dietary Restrictions
- Health Notes
- Calorie Goal
- Food Preferences (free-text field)

## Benefits

1. **Simpler UI**: Users can type preferences naturally instead of selecting from predefined list
2. **More Flexible**: Users can express nuanced preferences (e.g., "loves chicken, prefers spicy food, dislikes mushrooms")
3. **Better AI Input**: Free-form text gives AI more context than predefined categories
4. **Reduced Complexity**: No need to maintain FoodPreference table or junction table
5. **Consistent UX**: Food Preferences now matches Dietary Restrictions and Health Notes pattern

## Migration Strategy

### Option 1: Simple Migration (Recommended)
- Add FoodPreferences column
- Drop junction table
- Existing users start with empty preferences

### Option 2: Data Migration
- Add FoodPreferences column
- Migrate existing preferences: concatenate all preference names into text field
- Drop junction table

## Next Steps

1. Update HealthProfileController
2. Create and run database migration
3. Test all functionality
4. Update any remaining references to old FoodPreference system
5. Consider removing FoodPreference entity and table if no longer needed

## Files Modified

✅ Completed:
- src/MealPrepService.DataAccessLayer/Entities/HealthProfile.cs
- src/MealPrepService.DataAccessLayer/Data/MealPrepDbContext.cs
- src/MealPrepService.BusinessLogicLayer/DTOs/HealthProfileDto.cs
- src/MealPrepService.BusinessLogicLayer/Interfaces/IHealthProfileService.cs
- src/MealPrepService.BusinessLogicLayer/Services/HealthProfileService.cs
- src/MealPrepService.BusinessLogicLayer/Services/CustomerProfileAnalyzer.cs
- src/MealPrepService.BusinessLogicLayer/Services/OpenAIRecommendationService.cs
- src/MealPrepService.Web/PresentationLayer/ViewModels/HealthProfileViewModel.cs
- src/MealPrepService.Web/PresentationLayer/Views/HealthProfile/Edit.cshtml

⏳ Pending:
- src/MealPrepService.Web/PresentationLayer/Controllers/HealthProfileController.cs
- Database migration file (to be created)

## Build Status
✅ DataAccessLayer: Builds successfully
✅ BusinessLogicLayer: Builds successfully  
⏳ Web: Needs controller updates before building
