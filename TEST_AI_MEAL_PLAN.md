# Test AI Meal Plan Generation - Quick Guide

## ✅ Setup Complete!

Your AI-powered meal plan generation is now fully configured and ready to test.

## 🧪 Quick Test (5 minutes)

### Step 1: Prepare Your Profile
1. Open browser: `http://localhost:5020`
2. Login as a customer
3. Go to **Health Profile**
4. Set some preferences:
   - Dietary Restrictions: "vegetarian" or "low-carb"
   - Calorie Goal: 2000
   - Add an allergy (e.g., "peanuts")
   - Add food preferences

### Step 2: Create AI Meal Plan
1. Go to **Meal Plans** → **Create New**
2. Fill in the form:
   - Plan Name: "AI Test Plan"
   - Start Date: Today
   - End Date: 7 days from today
3. **✅ CHECK the "Generate with AI" checkbox**
4. Click **"Create Meal Plan"**

### Step 3: Verify Results
You should see:
- ✅ Meal plan created with meals
- ✅ 3 meals per day (Breakfast, Lunch, Dinner)
- ✅ 1-2 recipes per meal
- ✅ Recipes match your dietary restrictions
- ✅ No recipes with your allergens

## 📊 Check the Logs

Open the latest log file or check console output:

### ✅ Success Messages
```
[INF] Starting AI meal plan generation for account...
[INF] Generating AI meal plan recommendations...
[INF] Generated 21 meal recommendations for customer...
[INF] Created breakfast meal for 2026-01-23 with 2 recipes (AI recommended)
[INF] AI meal plan generated successfully
```

### ⚠️ Error Messages (if AI fails)
```
[ERR] AI is disabled but this is an AI-only system
[ERR] AI service is unavailable for customer...
[ERR] AI recommendation generation failed for customer...
```

## 🔍 What to Look For

### In the Meal Plan Details Page
- **Plan Name:** Shows "AI Meal Plan..." or your custom name
- **Is AI Generated:** Should show "Yes" or have an AI badge
- **Meals:** Should have 21 meals (7 days × 3 meals)
- **Recipes:** Each meal should have 1-2 recipes
- **Nutrition:** Should show total calories, protein, carbs, fat

### In Each Meal
- **Meal Type:** Breakfast, Lunch, or Dinner
- **Serve Date:** Correct date within plan range
- **Recipes:** Recipe names and nutrition info
- **No Allergens:** Verify no recipes contain your allergens

## 🐛 Troubleshooting

### Problem: Plan created but no meals
**Solution:**
1. Check logs for errors
2. Verify API key is set:
   ```powershell
   cd src/MealPrepService.Web
   dotnet user-secrets list
   ```
3. Should show:
   ```
   AI:OpenAI:ApiKey = sk-proj-...
   AI:UseRealAI = true
   ```

### Problem: "Generate with AI" checkbox not working
**Solution:**
1. Make sure you **checked the checkbox** before submitting
2. Check browser console for JavaScript errors (F12)
3. Verify form is submitting to `/MealPlan/GenerateAI`

### Problem: Getting error messages instead of meal plans
**Possible Causes:**
- AI is disabled (`UseRealAI = false`)
- API key is invalid
- OpenAI API error (rate limit, no credits)
- Network issue

**Check:**
1. Verify API key is valid at https://platform.openai.com/
2. Check OpenAI usage dashboard for errors
3. Look at logs for specific error messages
4. Ensure `UseRealAI = true` in configuration

## 💰 Monitor Costs

After testing, check your OpenAI usage:
1. Go to https://platform.openai.com/usage
2. Look for recent API calls
3. Verify costs are as expected (~$0.01-$0.03 per 7-day plan)

## 📝 Test Scenarios

### Scenario 1: Vegetarian with Peanut Allergy
1. Set dietary restriction: "vegetarian"
2. Add allergy: "peanuts"
3. Generate 7-day plan
4. **Verify:** No meat, no peanuts

### Scenario 2: Low-Carb Diet
1. Set dietary restriction: "low-carb"
2. Set calorie goal: 1800
3. Generate 7-day plan
4. **Verify:** Recipes are low in carbs

### Scenario 3: Multiple Allergies
1. Add allergies: "dairy", "eggs", "gluten"
2. Generate 7-day plan
3. **Verify:** No recipes with these ingredients

## 🎯 Expected Behavior

### With AI Enabled (UseRealAI = true)
- Uses GPT-4o-mini for recommendations
- Personalized based on profile
- AI reasoning in logs
- Cost: ~$0.01-$0.03 per plan

### With AI Disabled (UseRealAI = false)
- System throws exception - AI is required
- No meal plan creation
- User sees error message

### With AI Error (API failure)
- System throws exception - no fallback
- Logs error message
- User sees error message

## ✅ Success Criteria

Your AI meal plan generation is working if:
- ✅ Meals are created (not empty plan)
- ✅ Recipes match dietary restrictions
- ✅ No allergen recipes included
- ✅ Logs show AI recommendations
- ✅ OpenAI usage dashboard shows API calls

## 🚀 Next Steps

Once testing is successful:
1. Test with different dietary restrictions
2. Test with multiple customers
3. Monitor costs and usage
4. Adjust rate limits if needed
5. Consider adding more AI features

---

**Need Help?**
- Check `AI_MEAL_PLAN_SETUP.md` for detailed documentation
- Check `GPT_STATUS_REPORT.md` for configuration status
- Check logs in `src/MealPrepService.Web/logs/`
