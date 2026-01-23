# Active Meal Plan Feature

## Overview
Implemented an "Active Meal Plan" feature that allows customers to designate one meal plan as active. The grocery list generation now automatically uses the active meal plan, eliminating the need to manually select a plan each time.

## Changes Made

### 1. Database Layer

#### Entity Updates
- **File**: `src/MealPrepService.DataAccessLayer/Entities/MealPlan.cs`
- **Change**: Added `IsActive` boolean property
- **Impact**: Tracks which meal plan is currently active for each customer

### 2. Business Logic Layer

#### DTO Updates
- **File**: `src/MealPrepService.BusinessLogicLayer/DTOs/MealPlanDto.cs`
- **Change**: Added `IsActive` property

#### Interface Updates
- **File**: `src/MealPrepService.BusinessLogicLayer/Interfaces/IMealPlanService.cs`
- **Added Methods**:
  - `GetActivePlanAsync(Guid accountId)` - Get the active meal plan for an account
  - `SetActivePlanAsync(Guid planId, Guid accountId)` - Set a meal plan as active

- **File**: `src/MealPrepService.BusinessLogicLayer/Interfaces/IFridgeService.cs`
- **Added Method**:
  - `GenerateGroceryListFromActivePlanAsync(Guid accountId)` - Generate grocery list from active plan

#### Service Implementations

**MealPlanService** (`src/MealPrepService.BusinessLogicLayer/Services/MealPlanService.cs`):
- **GetActivePlanAsync()**: Retrieves the active meal plan for an account
- **SetActivePlanAsync()**: 
  - Deactivates all existing plans for the account
  - Activates the selected plan
  - Ensures only one plan is active at a time
- **MapToDto()**: Updated to include `IsActive` property

**FridgeService** (`src/MealPrepService.BusinessLogicLayer/Services/FridgeService.cs`):
- **GenerateGroceryListFromActivePlanAsync()**: 
  - Finds the active meal plan
  - Generates grocery list automatically
  - Throws helpful error if no active plan exists
- **GenerateGroceryListFromMealPlanAsync()**: 
  - Refactored common logic into private method
  - Used by both manual and active plan generation

### 3. Presentation Layer

#### ViewModel Updates
- **File**: `src/MealPrepService.Web/PresentationLayer/ViewModels/MealPlanViewModel.cs`
- **Change**: Added `IsActive` property with display attribute

- **File**: `src/MealPrepService.Web/PresentationLayer/ViewModels/FridgeViewModel.cs`
- **Change**: Added `IsActive` property to `MealPlanSelectionViewModel`

#### Controller Updates

**MealPlanController** (`src/MealPrepService.Web/PresentationLayer/Controllers/MealPlanController.cs`):
- **SetActive Action** (POST):
  - Allows users to set a meal plan as active
  - Shows success message
  - Redirects to meal plans index
- **MapToViewModel()**: Updated to include `IsActive` property

**FridgeController** (`src/MealPrepService.Web/PresentationLayer/Controllers/FridgeController.cs`):
- **GroceryList Action** (GET):
  - **Auto-generation**: If active plan exists, automatically generates grocery list
  - **Fallback**: Shows meal plan selection form if no active plan
  - **User-friendly messages**: Informs users about active plan status

#### View Updates

**Meal Plan Index** (`src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Index.cshtml`):
- **Visual Indicators**:
  - Active plans have green border (3px)
  - "ACTIVE" badge with checkmark icon in green
  - Badge displayed alongside AI/Manual badge
- **Set Active Button**:
  - Shows "Set Active" button for non-active plans
  - Button hidden for already-active plan
  - Uses form POST for security
- **Layout**: Responsive button layout with flex-wrap

## User Experience Flow

### Setting an Active Plan
1. User navigates to "My Meal Plans"
2. User sees all meal plans with visual indicators
3. Active plan shows green border and "ACTIVE" badge
4. User clicks "Set Active" on desired plan
5. System deactivates other plans and activates selected one
6. Success message confirms activation

### Generating Grocery List
1. User navigates to "Virtual Fridge" → "Grocery List"
2. **If active plan exists**:
   - Grocery list automatically generated
   - Shows which plan was used
   - No manual selection needed
3. **If no active plan**:
   - Shows meal plan selection form
   - Helpful message guides user to set active plan
   - Can still manually select a plan

## Benefits

1. **Convenience**: No need to select meal plan every time
2. **Consistency**: Always uses the same plan until changed
3. **Clear Visual Feedback**: Easy to see which plan is active
4. **Flexibility**: Can still manually select different plans if needed
5. **User-Friendly**: Helpful messages guide users through the process

## Technical Details

### Business Rules
- Only one meal plan can be active per account at a time
- Setting a plan as active automatically deactivates others
- Active status persists across sessions
- Deleting an active plan removes the active status

### Security
- Authorization checks ensure users can only modify their own plans
- CSRF protection on all state-changing operations
- Proper error handling and logging

### Database Migration Required
After deploying this feature, run the following migration:
```bash
dotnet ef migrations add AddIsActiveToMealPlan --project src/MealPrepService.DataAccessLayer --startup-project src/MealPrepService.Web
dotnet ef database update --project src/MealPrepService.DataAccessLayer --startup-project src/MealPrepService.Web
```

## Testing Recommendations

1. **Set Active Plan**:
   - Create multiple meal plans
   - Set one as active
   - Verify only one is active
   - Check visual indicators

2. **Grocery List Generation**:
   - Set a plan as active
   - Navigate to grocery list
   - Verify automatic generation
   - Check correct plan is used

3. **Edge Cases**:
   - No meal plans exist
   - No active plan set
   - Delete active plan
   - Switch active plans

4. **Authorization**:
   - Try to set another user's plan as active
   - Verify proper error handling

## Future Enhancements

1. **Auto-activation**: Automatically set first plan as active
2. **Expiry handling**: Auto-deactivate expired plans
3. **Notifications**: Remind users to set active plan
4. **Quick switch**: Toggle active plan from grocery list page
