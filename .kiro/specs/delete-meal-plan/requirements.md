# Delete Meal Plan - Requirements

## Feature Overview
Enable users to delete meal plans they no longer need, providing a clean way to manage their meal planning history.

## User Stories

### US-1: Customer Deletes Own Meal Plan
**As a** customer  
**I want to** delete my meal plans  
**So that** I can remove outdated or unwanted meal plans from my account

**Acceptance Criteria:**
1. Customer can view a delete option for their own meal plans
2. Customer is prompted to confirm deletion before the meal plan is removed
3. Upon confirmation, the meal plan and all associated meals are deleted
4. Customer receives confirmation that the deletion was successful
5. Customer cannot delete meal plans belonging to other users

### US-2: Manager Deletes Any Meal Plan
**As a** manager  
**I want to** delete any meal plan in the system  
**So that** I can manage and clean up meal plans as needed

**Acceptance Criteria:**
1. Manager can delete any meal plan regardless of ownership
2. Manager is prompted to confirm deletion before the meal plan is removed
3. Upon confirmation, the meal plan and all associated meals are deleted
4. Manager receives confirmation that the deletion was successful

### US-3: Cascade Deletion of Related Data
**As a** system  
**I want to** automatically delete all related data when a meal plan is deleted  
**So that** there are no orphaned records in the database

**Acceptance Criteria:**
1. When a meal plan is deleted, all associated meals are deleted
2. When meals are deleted, all associated meal-recipe relationships are deleted
3. Recipes themselves are NOT deleted (they may be used in other meal plans)
4. The deletion operation is atomic (all or nothing)

## Business Rules

1. **Authorization**: Only the meal plan owner or a manager can delete a meal plan
2. **Cascade Deletion**: Deleting a meal plan must cascade to delete all associated meals and meal-recipe relationships
3. **Confirmation Required**: Users must confirm deletion to prevent accidental data loss
4. **Audit Trail**: Deletion should be logged for audit purposes
5. **No Soft Delete**: This is a hard delete operation (permanent removal from database)

## Technical Requirements

1. Add `DeleteAsync(Guid planId)` method to `IMealPlanService` interface
2. Implement deletion logic in `MealPlanService` with proper authorization checks
3. Add DELETE action to `MealPlanController` with authorization filters
4. Create confirmation UI in the meal plan views
5. Ensure proper cascade deletion is configured in Entity Framework relationships
6. Add appropriate error handling for deletion failures
7. Log deletion operations for audit purposes

## Non-Functional Requirements

1. **Performance**: Deletion should complete within 2 seconds
2. **Security**: Authorization must be enforced at both controller and service layers
3. **Reliability**: Deletion must be transactional (all or nothing)
4. **Usability**: Confirmation dialog should clearly explain what will be deleted
5. **Logging**: All deletion attempts (successful and failed) should be logged

## Out of Scope

1. Soft delete functionality (marking as deleted but keeping in database)
2. Undo/restore deleted meal plans
3. Bulk deletion of multiple meal plans
4. Archiving meal plans instead of deleting
