# Delete Meal Plan - Implementation Tasks

## Task List

- [x] 1. Verify Database Cascade Configuration
  - [x] 1.1 Check MealPlan -> Meals cascade delete configuration
  - [x] 1.2 Check Meal -> MealRecipes cascade delete configuration
  - [x] 1.3 Add migration if cascade configuration is missing

- [x] 2. Update Business Logic Layer
  - [x] 2.1 Add DeleteAsync method to IMealPlanService interface
  - [x] 2.2 Implement DeleteAsync in MealPlanService with authorization
  - [x] 2.3 Add appropriate error handling and logging

- [x] 3. Update Presentation Layer - Controller
  - [x] 3.1 Add Delete GET action to MealPlanController (show confirmation)
  - [x] 3.2 Add DeleteConfirmed POST action to MealPlanController
  - [x] 3.3 Add authorization checks in controller actions

- [x] 4. Update Presentation Layer - Views
  - [x] 4.1 Create Delete.cshtml confirmation view
  - [x] 4.2 Add delete button to Index.cshtml
  - [x] 4.3 Add delete button to Details.cshtml

- [x] 5. Write Unit Tests
  - [x] 5.1 Test DeleteAsync with valid owner
  - [x] 5.2 Test DeleteAsync with manager role
  - [x] 5.3 Test DeleteAsync with unauthorized user (should throw)
  - [x] 5.4 Test DeleteAsync with non-existent meal plan (should throw)

- [x] 6. Write Property-Based Tests
  - [x] 6.1 Property test: Deletion completeness (meal plan and meals removed)
  - [x] 6.2 Property test: Authorization enforcement
  - [x] 6.3 Property test: Cascade integrity (recipes not deleted)
  - [x] 6.4 Property test: Transactional atomicity

- [x] 7. Manual Testing
  - [x] 7.1 Test delete as customer (own meal plan)
  - [x] 7.2 Test delete as customer (other's meal plan - should fail)
  - [x] 7.3 Test delete as manager (any meal plan)
  - [x] 7.4 Verify cascade deletion in database
  - [x] 7.5 Test cancel button on confirmation page

## Task Details

### Task 1: Verify Database Cascade Configuration
**Files**: `src/MealPrepService.DataAccessLayer/Data/MealPrepDbContext.cs`

Check if cascade delete is properly configured for:
- MealPlan -> Meals relationship
- Meal -> MealRecipes relationship

If not configured, add the configuration in `OnModelCreating` method.

### Task 2: Update Business Logic Layer
**Files**: 
- `src/MealPrepService.BusinessLogicLayer/Interfaces/IMealPlanService.cs`
- `src/MealPrepService.BusinessLogicLayer/Services/MealPlanService.cs`

Add the delete method signature to the interface and implement it in the service with:
- Meal plan retrieval
- Authorization check (owner or manager)
- Deletion with cascade
- Logging

### Task 3: Update Presentation Layer - Controller
**Files**: `src/MealPrepService.Web/PresentationLayer/Controllers/MealPlanController.cs`

Add two actions:
- GET Delete: Show confirmation page
- POST DeleteConfirmed: Execute deletion

Both should include authorization checks.

### Task 4: Update Presentation Layer - Views
**Files**: 
- `src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Delete.cshtml` (new)
- `src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Index.cshtml`
- `src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Details.cshtml`

Create the confirmation view and add delete buttons to existing views.

### Task 5: Write Unit Tests
**Files**: `tests/MealPrepService.Tests/MealPlanServiceTests.cs` (new or existing)

Write unit tests covering:
- Happy path (owner deletes own plan)
- Manager deletes any plan
- Unauthorized deletion attempt
- Non-existent meal plan

### Task 6: Write Property-Based Tests
**Files**: `tests/MealPrepService.Tests/MealPlanServicePropertyTests.cs`

Write property-based tests using FsCheck to verify:
- Deletion completeness
- Authorization rules
- Cascade behavior
- Transaction atomicity

### Task 7: Manual Testing
Perform manual testing in the running application to verify:
- UI works correctly
- Authorization is enforced
- Cascade deletion works
- User feedback is appropriate
