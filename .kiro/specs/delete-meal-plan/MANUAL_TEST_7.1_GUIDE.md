# Manual Test 7.1: Test Delete as Customer (Own Meal Plan)

## Test Objective
Verify that a customer can successfully delete their own meal plan through the UI, with proper confirmation and feedback.

## Prerequisites
- Application must be running
- Database must be initialized (with admin account and sample ingredients)
- A customer account must exist
- At least one meal plan must exist for the customer

## Test Environment Setup

### Step 1: Start the Application

```bash
dotnet run --project src/MealPrepService.Web
```

Wait for the application to start. You should see output indicating the application is running, typically on `https://localhost:5001` or `http://localhost:5000`.

### Step 2: Create a Customer Account (if not already exists)

1. Open your browser and navigate to: `https://localhost:5001` (or the port shown in terminal)
2. Click **"Register"** in the navigation menu
3. Fill in the registration form:
   - **Email**: `customer@test.com`
   - **Password**: `Customer@123`
   - **Confirm Password**: `Customer@123`
   - **Full Name**: `Test Customer`
   - **Role**: Select **"Customer"**
4. Click **"Register"** button
5. You should be automatically logged in after registration

### Step 3: Create a Meal Plan (if not already exists)

1. Navigate to **"Meal Plans"** from the menu
2. Click **"Create New Meal Plan"**
3. Fill in the form:
   - **Plan Name**: `My Weekly Plan`
   - **Start Date**: Select today's date
   - **End Date**: Select a date 7 days from today
4. Click **"Create Manual Plan"** button
5. You should be redirected to the meal plan details page
6. Note the meal plan ID from the URL (e.g., `https://localhost:5001/MealPlan/Details/{guid}`)

**Optional**: Add some meals to the plan to make the test more realistic:
1. From the meal plan details page, click **"Add Meal"**
2. Select a meal type (Breakfast, Lunch, Dinner, Snack)
3. Select a serve date within the plan's date range
4. Select one or more recipes (if available)
5. Click **"Add Meal"**

## Test Execution

### Test Case 7.1: Delete Own Meal Plan as Customer

#### Test Steps:

**Step 1: Navigate to Meal Plans List**
1. Ensure you are logged in as the customer (`customer@test.com`)
2. Navigate to **"Meal Plans"** from the menu
3. **Expected Result**: You should see a list of your meal plans, including "My Weekly Plan"

**Step 2: Access Delete Confirmation Page**
1. Locate the meal plan you want to delete ("My Weekly Plan")
2. Click the **"Delete"** button next to the meal plan (or from the details page)
3. **Expected Result**: 
   - You are redirected to a delete confirmation page
   - The page displays a warning message about the action being irreversible
   - The page shows the meal plan details (name, date range, type, number of meals)
   - Two buttons are visible: "Delete Meal Plan" (red/danger) and "Cancel"

**Step 3: Verify Confirmation Page Content**
1. Review the confirmation page
2. **Expected Result**:
   - Warning message states: "Are you sure you want to delete this meal plan?"
   - Warning explains: "This action cannot be undone. All meals and recipes associated with this plan will be removed."
   - Meal plan details are displayed:
     - Plan Name: "My Weekly Plan"
     - Date Range: Shows the start and end dates
     - Type: "Manual" (or "AI Generated" if applicable)
     - Total Meals: Shows the count of meals in the plan

**Step 4: Test Cancel Button**
1. Click the **"Cancel"** button
2. **Expected Result**:
   - You are redirected back to the meal plan details page
   - The meal plan is NOT deleted
   - No success or error messages are displayed

**Step 5: Delete the Meal Plan**
1. Navigate back to the delete confirmation page (repeat Step 2)
2. Click the **"Delete Meal Plan"** button
3. **Expected Result**:
   - You are redirected to the meal plans list page (Index)
   - A success message is displayed: "Meal plan deleted successfully!"
   - The deleted meal plan is no longer visible in the list

**Step 6: Verify Deletion in Database**
1. Try to access the deleted meal plan directly by URL (if you noted the ID earlier)
   - Navigate to: `https://localhost:5001/MealPlan/Details/{deleted-plan-id}`
2. **Expected Result**:
   - You receive a "Not Found" error or are redirected to the meal plans list
   - An error message may be displayed: "Meal plan not found."

**Step 7: Verify Cascade Deletion (Optional - Database Check)**
1. If you have database access tools (e.g., DB Browser for SQLite):
   - Open the database file: `src/MealPrepService.Web/mealprepservice.db`
   - Query the `MealPlans` table: `SELECT * FROM MealPlans WHERE Id = '{deleted-plan-id}'`
   - **Expected Result**: No records found
   - Query the `Meals` table: `SELECT * FROM Meals WHERE PlanId = '{deleted-plan-id}'`
   - **Expected Result**: No records found (all meals were cascade deleted)
   - Query the `MealRecipes` table for any orphaned records
   - **Expected Result**: No orphaned meal-recipe relationships

## Test Results

### Pass Criteria
- ✅ Customer can access the delete confirmation page for their own meal plan
- ✅ Confirmation page displays appropriate warning and meal plan details
- ✅ Cancel button returns to details page without deleting
- ✅ Delete button successfully removes the meal plan
- ✅ Success message is displayed after deletion
- ✅ Deleted meal plan is no longer accessible
- ✅ Associated meals are cascade deleted (if database check performed)

### Fail Criteria
- ❌ Customer cannot access delete confirmation page
- ❌ Confirmation page is missing or displays incorrect information
- ❌ Cancel button deletes the meal plan
- ❌ Delete button fails to remove the meal plan
- ❌ No success message is displayed
- ❌ Deleted meal plan is still accessible
- ❌ Associated meals are not deleted (orphaned records)

## Test Data Cleanup

After completing the test, you may want to:
1. Create a new meal plan for future tests
2. Or reset the database by deleting `src/MealPrepService.Web/mealprepservice.db` and restarting the application

## Troubleshooting

### Issue: Cannot register a customer account
- **Solution**: Check if the email is already registered. Try a different email or reset the database.

### Issue: No recipes available when adding meals
- **Solution**: You need to create recipes first. Navigate to "Recipes" and create some sample recipes, or skip adding meals to the plan.

### Issue: Application won't start
- **Solution**: 
  - Ensure you're in the correct directory
  - Check if another instance is already running
  - Verify .NET SDK is installed: `dotnet --version`
  - Check for port conflicts

### Issue: Delete button not visible
- **Solution**: 
  - Ensure you're logged in as the customer who owns the meal plan
  - Check the browser console for JavaScript errors
  - Verify the view files were updated correctly

## Notes
- This test validates **US-1** (Customer Deletes Own Meal Plan) from the requirements
- The test confirms authorization is working (customer can only delete their own plans)
- The test verifies cascade deletion is functioning correctly
- The test checks user feedback and confirmation flow

## Related Tests
- **Task 7.2**: Test delete as customer (other's meal plan - should fail)
- **Task 7.3**: Test delete as manager (any meal plan)
- **Task 7.4**: Verify cascade deletion in database
- **Task 7.5**: Test cancel button on confirmation page
