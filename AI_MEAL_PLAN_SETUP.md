# AI-Powered Meal Plan Generation - Setup Complete ✅

## What Was Fixed

### Problem
When customers created a meal plan with "Generate with AI" checked, the system:
- Created an empty meal plan with no meals
- Did not use GPT/AI for recommendations
- Used simple random selection instead

### Solution Implemented

#### 1. Updated MealPlanService
- **Added IAIRecommendationService dependency** to leverage GPT recommendations
- **Refactored `GenerateAiMealPlanAsync`** to call AI service for meal recommendations
- **AI-only system** - throws exceptions if AI fails or is disabled
- **Created `CreateMealsFromRecommendations`** method to convert AI recommendations into meals with recipes

#### 2. Updated AIRecommendationService
- **Added `GetMealPlanRecommendationsAsync`** method to generate recommendations for entire meal plans
- Generates recommendations for each day and meal type (Breakfast, Lunch, Dinner)
- Returns 1-2 recipes per meal based on customer profile

#### 3. Updated MealRecommendation DTO
- **Added properties for meal plan generation:**
  - `Date` - when the meal should be served
  - `MealType` - breakfast, lunch, or dinner
  - `RecommendedRecipeIds` - list of recipe IDs for the meal

#### 4. Fixed Create View
- **Added JavaScript** to dynamically change form action based on checkbox
- When "Generate with AI" is checked → submits to `GenerateAI` action
- When unchecked → submits to `Create` action (manual)

## How It Works Now

### User Flow
1. Customer goes to **Create Meal Plan**
2. Enters plan name, start date, end date
3. **Checks "Generate with AI" checkbox**
4. Clicks "Create Meal Plan"
5. System calls `GenerateAI` action

### Backend Flow
```
GenerateAI Controller Action
    ↓
MealPlanService.GenerateAiMealPlanAsync()
    ↓
AIRecommendationService.GetMealPlanRecommendationsAsync()
    ↓
AIRecommendationEngine.GenerateRecommendationsAsync()
    ↓
OpenAIRecommendationService (GPT-4o-mini)
    ↓
Returns personalized meal recommendations
    ↓
MealPlanService creates Meals with Recipes
    ↓
Saves to database
```

### AI Integration
- **Uses GPT-4o-mini** for cost-effective recommendations
- **Analyzes customer profile:**
  - Health goals
  - Dietary restrictions
  - Allergies (automatically excluded)
  - Food preferences
- **Generates 3 meals per day** (breakfast, lunch, dinner)
- **1-2 recipes per meal** for variety

### Error Handling
If AI fails or is disabled:
- System throws exceptions with clear error messages
- No meal plan is created
- User sees error message explaining the issue
- Logs contain detailed error information
- Logs warning message

## Testing

### Test AI Meal Plan Generation

1. **Ensure you have a health profile:**
   - Go to Health Profile
   - Set dietary restrictions (e.g., vegetarian, low-carb)
   - Add allergies if any
   - Add food preferences

2. **Create AI Meal Plan:**
   - Go to Meal Plans → Create New
   - Enter plan name: "AI Test Plan"
   - Set date range (e.g., 7 days)
   - **Check "Generate with AI"**
   - Click "Create Meal Plan"

3. **Verify Results:**
   - Plan should have meals for each day
   - Each meal should have 1-2 recipes
   - Recipes should match your dietary restrictions
   - No recipes with your allergens

### Check Logs

Look for these messages:

✅ **AI Working:**
```
[INF] Starting AI meal plan generation for account {AccountId}
[INF] Generating AI meal plan recommendations for customer {CustomerId}
[INF] Generated {Count} meal recommendations for customer {CustomerId}
[INF] Created {MealType} meal for {Date} with {RecipeCount} recipes (AI recommended)
[INF] AI meal plan generated successfully for account {AccountId}
```

⚠️ **AI Errors:**
```
[ERR] No AI recommendations returned for account...
[ERR] Error during AI meal generation for account...
[ERR] AI meal plan generation failed: ...
```

## Configuration

### AI Settings (appsettings.json)
```json
"AI": {
  "UseRealAI": false,  // Overridden by user secrets
  "OpenAI": {
    "ApiKey": "",  // Set via user secrets
    "Model": "gpt-4o-mini",
    "Endpoint": "https://api.openai.com/v1/chat/completions"
  }
}
```

### User Secrets (Required)
```powershell
cd src/MealPrepService.Web
dotnet user-secrets set "AI:OpenAI:ApiKey" "your-key-here"
dotnet user-secrets set "AI:UseRealAI" "true"
```

## Cost Estimate

### Per Meal Plan (7 days, 3 meals/day = 21 meals)
- **Input tokens:** ~500-1000 per meal × 21 = ~10,500-21,000 tokens
- **Output tokens:** ~300-500 per meal × 21 = ~6,300-10,500 tokens
- **Estimated cost:** $0.01 - $0.03 per 7-day meal plan

### Monthly Usage (100 customers, 4 plans each)
- **400 meal plans/month**
- **Estimated cost:** $4 - $12/month

## Files Modified

1. **src/MealPrepService.BusinessLogicLayer/Services/MealPlanService.cs**
   - Added AI service integration
   - Refactored meal generation logic

2. **src/MealPrepService.BusinessLogicLayer/Services/AIRecommendationService.cs**
   - Added `GetMealPlanRecommendationsAsync` method

3. **src/MealPrepService.BusinessLogicLayer/Interfaces/IAIRecommendationService.cs**
   - Added interface method

4. **src/MealPrepService.BusinessLogicLayer/DTOs/MealRecommendation.cs**
   - Added Date, MealType, RecommendedRecipeIds properties

5. **src/MealPrepService.Web/PresentationLayer/Views/MealPlan/Create.cshtml**
   - Added JavaScript to handle form submission

## Troubleshooting

### Issue: No meals created
**Check:**
- Is "Generate with AI" checkbox checked?
- Check logs for errors
- Verify AI is enabled in user secrets

### Issue: Using fallback instead of AI
**Check:**
- API key is valid
- `AI:UseRealAI` is set to `true`
- Check logs for AI errors
- Verify OpenAI account has credits

### Issue: Meals created but not personalized
**Check:**
- Customer has a health profile
- Health profile has dietary restrictions/preferences
- Check logs for AI recommendation details

## Next Steps

### Optional Enhancements
- [ ] Add meal type preferences (e.g., light breakfast, heavy dinner)
- [ ] Add cuisine preferences (Italian, Asian, etc.)
- [ ] Add calorie targets per meal
- [ ] Add macro distribution preferences
- [ ] Allow regenerating specific meals
- [ ] Add AI explanation for each meal choice

---

**Status:** ✅ READY TO USE  
**Last Updated:** January 23, 2026  
**AI Integration:** Complete with GPT-4o-mini
