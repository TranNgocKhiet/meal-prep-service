# Delete Meal Plan - Design Document

## Architecture Overview

This feature adds delete functionality to the existing meal plan system following the three-layer architecture pattern already established in the application.

### Layers Affected
1. **Data Access Layer**: Ensure cascade delete is properly configured
2. **Business Logic Layer**: Add delete method to service interface and implementation
3. **Presentation Layer**: Add delete UI and controller action

## Component Design

### 1. Data Access Layer

#### Database Cascade Configuration
The existing Entity Framework relationships should already support cascade delete, but we'll verify:

```csharp
// In MealPrepDbContext.cs - Verify these relationships exist
modelBuilder.Entity<MealPlan>()
    .HasMany(mp => mp.Meals)
    .WithOne(m => m.MealPlan)
    .HasForeignKey(m => m.PlanId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<Meal>()
    .HasMany(m => m.MealRecipes)
    .WithOne(mr => mr.Meal)
    .HasForeignKey(mr => mr.MealId)
    .OnDelete(DeleteBehavior.Cascade);
```

### 2. Business Logic Layer

#### Interface Update
Add to `IMealPlanService`:

```csharp
Task DeleteAsync(Guid planId, Guid requestingAccountId);
```

#### Service Implementation
Add to `MealPlanService`:

```csharp
public async Task DeleteAsync(Guid planId, Guid requestingAccountId)
{
    // 1. Retrieve meal plan
    var mealPlan = await _unitOfWork.MealPlans.GetByIdAsync(planId);
    
    if (mealPlan == null)
    {
        throw new NotFoundException($"Meal plan with ID {planId} not found");
    }
    
    // 2. Authorization check (service layer)
    // Note: Controller will also check, but defense in depth
    var requestingAccount = await _unitOfWork.Accounts.GetByIdAsync(requestingAccountId);
    
    if (requestingAccount == null)
    {
        throw new AuthenticationException("Requesting account not found");
    }
    
    // Check if user owns the plan or is a manager
    if (mealPlan.AccountId != requestingAccountId && requestingAccount.Role != "Manager")
    {
        throw new AuthorizationException("You don't have permission to delete this meal plan");
    }
    
    // 3. Delete the meal plan (cascade will handle meals and meal-recipes)
    _unitOfWork.MealPlans.Remove(mealPlan);
    await _unitOfWork.SaveChangesAsync();
    
    // 4. Log the deletion
    _logger.LogInformation(
        "Meal plan {PlanId} ({PlanName}) deleted by account {AccountId}",
        planId, mealPlan.PlanName, requestingAccountId);
}
```

### 3. Presentation Layer

#### Controller Action
Add to `MealPlanController`:

```csharp
// GET: MealPlan/Delete/{id} - Show delete confirmation
[HttpGet]
public async Task<IActionResult> Delete(Guid id)
{
    try
    {
        var mealPlanDto = await _mealPlanService.GetByIdAsync(id);
        
        if (mealPlanDto == null)
        {
            return NotFound("Meal plan not found.");
        }

        // Check authorization
        var accountId = GetCurrentAccountId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        if (mealPlanDto.AccountId != accountId && userRole != "Manager")
        {
            return Forbid("You don't have permission to delete this meal plan.");
        }

        var viewModel = MapToViewModel(mealPlanDto);
        return View(viewModel);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while loading delete confirmation for meal plan {MealPlanId}", id);
        TempData["ErrorMessage"] = "An error occurred while loading the meal plan.";
        return RedirectToAction(nameof(Index));
    }
}

// POST: MealPlan/Delete/{id} - Confirm deletion
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(Guid id)
{
    try
    {
        var accountId = GetCurrentAccountId();
        await _mealPlanService.DeleteAsync(id, accountId);
        
        _logger.LogInformation("Meal plan {MealPlanId} deleted successfully by account {AccountId}", id, accountId);
        
        TempData["SuccessMessage"] = "Meal plan deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
    catch (NotFoundException ex)
    {
        TempData["ErrorMessage"] = ex.Message;
        return RedirectToAction(nameof(Index));
    }
    catch (AuthorizationException ex)
    {
        TempData["ErrorMessage"] = ex.Message;
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred while deleting meal plan {MealPlanId}", id);
        TempData["ErrorMessage"] = "An error occurred while deleting the meal plan. Please try again.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
```

#### View Updates

**Delete.cshtml** (new view):
```cshtml
@model MealPlanViewModel

@{
    ViewData["Title"] = "Delete Meal Plan";
}

<div class="container mt-4">
    <h2>Delete Meal Plan</h2>
    
    <div class="alert alert-warning">
        <h4>Are you sure you want to delete this meal plan?</h4>
        <p>This action cannot be undone. All meals and recipes associated with this plan will be removed.</p>
    </div>

    <div class="card">
        <div class="card-body">
            <h5 class="card-title">@Model.PlanName</h5>
            <p class="card-text">
                <strong>Date Range:</strong> @Model.StartDate.ToString("yyyy-MM-dd") to @Model.EndDate.ToString("yyyy-MM-dd")<br />
                <strong>Type:</strong> @(Model.IsAiGenerated ? "AI Generated" : "Manual")<br />
                <strong>Total Meals:</strong> @Model.Meals.Count
            </p>
        </div>
    </div>

    <form asp-action="DeleteConfirmed" method="post" class="mt-3">
        <input type="hidden" asp-for="Id" />
        <button type="submit" class="btn btn-danger">Delete Meal Plan</button>
        <a asp-action="Details" asp-route-id="@Model.Id" class="btn btn-secondary">Cancel</a>
    </form>
</div>
```

**Update Index.cshtml** - Add delete button:
```cshtml
<!-- Add to the action buttons for each meal plan -->
<a asp-action="Delete" asp-route-id="@plan.Id" class="btn btn-sm btn-danger">Delete</a>
```

**Update Details.cshtml** - Add delete button:
```cshtml
<!-- Add to the action buttons section -->
<a asp-action="Delete" asp-route-id="@Model.Id" class="btn btn-danger">Delete Meal Plan</a>
```

## Error Handling

### Exception Types
1. **NotFoundException**: Meal plan doesn't exist
2. **AuthorizationException**: User doesn't have permission
3. **AuthenticationException**: User identity cannot be determined
4. **General Exception**: Database or unexpected errors

### User Feedback
- Success: "Meal plan deleted successfully!"
- Not Found: "Meal plan not found."
- Unauthorized: "You don't have permission to delete this meal plan."
- Error: "An error occurred while deleting the meal plan. Please try again."

## Security Considerations

1. **Authorization Checks**: Performed at both controller and service layers
2. **CSRF Protection**: Use `[ValidateAntiForgeryToken]` on POST action
3. **Role-Based Access**: Customers can only delete their own plans, managers can delete any
4. **Audit Logging**: All deletion attempts are logged with user and meal plan details

## Testing Strategy

### Unit Tests
1. Test service method with valid owner
2. Test service method with manager role
3. Test service method with unauthorized user
4. Test service method with non-existent meal plan
5. Test cascade deletion of meals and meal-recipes

### Integration Tests
1. Test full delete flow from controller to database
2. Verify cascade deletion in database
3. Test authorization at controller level

### Property-Based Tests
1. **Property 1**: Deleting a meal plan removes it from the database
2. **Property 2**: Deleting a meal plan cascades to delete all associated meals
3. **Property 3**: Only authorized users can delete meal plans
4. **Property 4**: Recipes are not deleted when meal plans are deleted

## Correctness Properties

### Property 1: Deletion Completeness
**Validates: Requirements US-3.1, US-3.2**

For any meal plan with ID `planId`:
- After successful deletion, `GetByIdAsync(planId)` returns null
- All meals with `PlanId == planId` are removed from database
- All meal-recipe relationships for those meals are removed

### Property 2: Authorization Enforcement
**Validates: Requirements US-1.5, BR-1**

For any deletion attempt:
- If requester is the owner OR requester is a manager, deletion succeeds
- If requester is neither owner nor manager, AuthorizationException is thrown
- Meal plan remains in database after failed authorization

### Property 3: Cascade Integrity
**Validates: Requirements US-3.3**

For any meal plan deletion:
- All associated meals are deleted
- All meal-recipe relationships are deleted
- Referenced recipes still exist in the database
- No orphaned meal or meal-recipe records remain

### Property 4: Transactional Atomicity
**Validates: Requirements NFR-3**

For any deletion operation:
- Either all related records are deleted OR none are deleted
- No partial deletion state exists
- Database remains consistent after operation

## Performance Considerations

1. **Single Query**: Use `GetByIdAsync` to retrieve meal plan
2. **Cascade Delete**: Let database handle cascade deletion efficiently
3. **Transaction**: Wrapped in UnitOfWork transaction for atomicity
4. **Expected Time**: < 2 seconds for typical meal plans (< 100 meals)

## Rollback Plan

If issues arise:
1. Remove delete button from UI
2. Comment out controller actions
3. Keep service method for potential future use
4. No database migration needed (no schema changes)
