# AI Meal Plan Generation - Technical Flow Documentation

## Overview

This document explains the complete technical flow of how AI-powered meal plan generation works in the MealPrepService application. This is a developer-focused guide showing the exact code path, classes, methods, and data flow.

## Architecture Overview

The AI meal plan generation uses a **layered architecture** with clear separation of concerns:

```
Presentation Layer (Controller)
    ↓
Business Logic Layer (Services)
    ↓
AI Services (OpenAI Integration)
    ↓
Data Access Layer (Repositories)
```

## Key Components

### 1. Service Interfaces

- **`IAIRecommendationService`** - Orchestrates the entire AI recommendation process
- **`ICustomerProfileAnalyzer`** - Analyzes customer health profile and preferences
- **`IRecommendationEngine`** - Generates recipe recommendations using AI
- **`ILLMService`** - Communicates with OpenAI GPT models
- **`IMealPlanService`** - Creates and manages meal plans

### 2. Service Implementations

- **`AIRecommendationService`** - Main orchestrator
- **`CustomerProfileAnalyzer`** - Profile analysis logic
- **`AIRecommendationEngine`** - AI-powered recommendation engine
- **`OpenAIRecommendationService`** - OpenAI API integration
- **`MealPlanService`** - Meal plan CRUD operations

## Complete Flow: Step-by-Step

### Step 1: User Initiates AI Meal Plan Generation

**File:** `src/MealPrepService.Web/PresentationLayer/Controllers/MealPlanController.cs`

**Method:** `GenerateAI(CreateMealPlanViewModel model)`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GenerateAI(CreateMealPlanViewModel model)
{
    var accountId = GetCurrentAccountId();
    
    // Call MealPlanService to generate AI meal plan
    var aiGeneratedPlan = await _mealPlanService.GenerateAiMealPlanAsync(
        accountId, 
        model.StartDate, 
        model.EndDate,
        model.PlanName);
    
    return RedirectToAction(nameof(Details), new { id = aiGeneratedPlan.Id });
}
```

**Input:**
- `accountId` (Guid) - Current logged-in customer
- `startDate` (DateTime) - Meal plan start date
- `endDate` (DateTime) - Meal plan end date
- `planName` (string) - Custom plan name

---

### Step 2: MealPlanService Creates Plan and Requests AI Recommendations

**File:** `src/MealPrepService.BusinessLogicLayer/Services/MealPlanService.cs`

**Method:** `GenerateAiMealPlanAsync(Guid accountId, DateTime startDate, DateTime endDate, string? customPlanName)`

```csharp
public async Task<MealPlanDto> GenerateAiMealPlanAsync(
    Guid accountId, DateTime startDate, DateTime endDate, string? customPlanName)
{
    // 1. Check meal plan limit (system configuration)
    var maxMealPlans = await _systemConfigService.GetMaxMealPlansPerCustomerAsync();
    var existingPlans = await _unitOfWork.MealPlans.GetByAccountIdAsync(accountId);
    if (existingPlans.Count() >= maxMealPlans)
    {
        throw new BusinessException($"Maximum limit of {maxMealPlans} meal plans reached");
    }

    // 2. Create empty meal plan entity
    var mealPlan = new MealPlan
    {
        Id = Guid.NewGuid(),
        AccountId = accountId,
        PlanName = customPlanName ?? $"AI Meal Plan {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
        StartDate = startDate,
        EndDate = endDate,
        IsAiGenerated = true,
        CreatedAt = DateTime.UtcNow
    };
    
    await _unitOfWork.MealPlans.AddAsync(mealPlan);
    await _unitOfWork.SaveChangesAsync();

    // 3. Get AI recommendations for entire date range
    var recommendations = await _aiRecommendationService.GetMealPlanRecommendationsAsync(
        accountId, startDate, endDate);

    // 4. Create meals from recommendations
    await CreateMealsFromRecommendations(mealPlan, recommendations);

    // 5. Return complete meal plan
    var fullPlan = await _unitOfWork.MealPlans.GetWithMealsAndRecipesAsync(mealPlan.Id);
    return MapToDto(fullPlan!);
}
```

**Key Actions:**
1. Validates meal plan limit (default: 5 plans per customer)
2. Creates empty meal plan in database
3. Requests AI recommendations for all days/meals
4. Populates meal plan with AI-recommended recipes
5. Returns complete meal plan DTO

---

### Step 3: AIRecommendationService Orchestrates the Process

**File:** `src/MealPrepService.BusinessLogicLayer/Services/AIRecommendationService.cs`

**Method:** `GetMealPlanRecommendationsAsync(Guid customerId, DateTime startDate, DateTime endDate)`

```csharp
public async Task<IEnumerable<MealRecommendation>> GetMealPlanRecommendationsAsync(
    Guid customerId, DateTime startDate, DateTime endDate)
{
    // 1. Check if AI is enabled
    if (!await IsAIEnabledAsync())
    {
        return Enumerable.Empty<MealRecommendation>();
    }

    // 2. Get AI configuration (min/max recommendations)
    var config = await _configService.GetConfigurationAsync();

    // 3. Analyze customer profile (health data, allergies, preferences)
    var customerContext = await _profileAnalyzer.AnalyzeCustomerAsync(customerId);

    // 4. Generate recommendations for each day and meal type
    var recommendations = new List<MealRecommendation>();
    var mealTypes = new[] { "Breakfast", "Lunch", "Dinner" };
    var currentDate = startDate;

    while (currentDate <= endDate)
    {
        foreach (var mealType in mealTypes)
        {
            // Generate diversity-aware recommendations for this specific meal
            var mealRecommendations = await _recommendationEngine.GenerateRecommendationsAsync(
                customerContext,
                1, // Min 1 recipe per meal
                1, // Max 1 recipe per meal for variety
                currentDate,
                mealType
            );

            if (mealRecommendations.Any())
            {
                var mealRecommendation = mealRecommendations.First();
                mealRecommendation.Date = currentDate;
                mealRecommendation.MealType = mealType;
                recommendations.Add(mealRecommendation);
            }
        }
        currentDate = currentDate.AddDays(1);
    }

    return recommendations;
}
```

**Key Actions:**
1. Verifies AI is enabled in system configuration
2. Retrieves AI configuration settings
3. Analyzes customer profile (Step 4)
4. Loops through each day and meal type (Breakfast, Lunch, Dinner)
5. Generates 1 recipe per meal using AI (Step 5)
6. Returns list of recommendations with date/meal type metadata

---

### Step 4: CustomerProfileAnalyzer Gathers Customer Data

**File:** `src/MealPrepService.BusinessLogicLayer/Services/CustomerProfileAnalyzer.cs`

**Method:** `AnalyzeCustomerAsync(Guid customerId)`

```csharp
public async Task<CustomerContext> AnalyzeCustomerAsync(Guid customerId)
{
    var context = new CustomerContext();

    // 1. Get customer account
    context.Customer = await _unitOfWork.Accounts.GetByIdAsync(customerId);

    // 2. Get health profile
    var allHealthProfiles = await _unitOfWork.HealthProfiles.GetAllAsync();
    context.HealthProfile = allHealthProfiles.FirstOrDefault(hp => hp.AccountId == customerId);
    
    if (context.HealthProfile != null)
    {
        // 3. Get allergies from health profile
        if (context.HealthProfile.Allergies != null && context.HealthProfile.Allergies.Any())
        {
            context.Allergies = context.HealthProfile.Allergies.ToList();
        }
    }

    // 4. Get order history (last 10 orders)
    var allOrders = await _unitOfWork.Orders.GetAllAsync();
    context.OrderHistory = allOrders
        .Where(o => o.AccountId == customerId)
        .OrderByDescending(o => o.OrderDate)
        .Take(10)
        .ToList();

    // 5. Determine if profile is complete
    context.HasCompleteProfile = context.HealthProfile != null && context.Allergies.Any();

    return context;
}
```

**CustomerContext Structure:**
```csharp
public class CustomerContext
{
    public Account Customer { get; set; }
    public HealthProfile? HealthProfile { get; set; }
    public List<Allergy> Allergies { get; set; } = new();
    public List<Order> OrderHistory { get; set; } = new();
    public bool HasCompleteProfile { get; set; }
    public List<string> MissingDataWarnings { get; set; } = new();
}
```

**Data Collected:**
- Customer name and account info
- Health profile:
  - Daily calorie goal
  - Dietary restrictions (text field)
  - Health notes (text field)
  - Food preferences (text field)
- Allergies list (e.g., "Peanuts", "Shellfish")
- Order history (for future personalization)

---

### Step 5: AIRecommendationEngine Generates Recipe Recommendations

**File:** `src/MealPrepService.BusinessLogicLayer/Services/AIRecommendationEngine.cs`

**Method:** `GenerateRecommendationsAsync(CustomerContext context, int minCount, int maxCount, DateTime targetDate, string mealType)`

```csharp
public async Task<List<MealRecommendation>> GenerateRecommendationsAsync(
    CustomerContext context, int minCount, int maxCount, 
    DateTime targetDate, string mealType)
{
    // 1. Get all recipes from database
    var allRecipes = await _unitOfWork.Recipes.GetAllAsync();
    var recipeList = allRecipes.ToList();

    // 2. Filter out recipes with customer's allergens (SAFETY FIRST)
    var safeRecipes = await FilterAllergens(recipeList, context);

    // 3. Get recent recipe history (last 3 days) to avoid repetition
    var threeDaysAgo = targetDate.AddDays(-3);
    var recentRecipeIds = await _unitOfWork.MealPlans.GetRecentRecipeIdsAsync(
        context.Customer.Id, threeDaysAgo, targetDate.AddDays(-1));

    // 4. Check if AI is enabled and available
    var useAI = _configuration.GetValue<bool>("AI:UseRealAI", false);
    var isAvailable = await _llmService.IsAvailableAsync();
    
    if (!useAI || !isAvailable)
    {
        throw new InvalidOperationException("AI service is unavailable");
    }

    // 5. Generate diversity-aware AI recommendations
    return await GenerateDiversityAwareRecommendationsAsync(
        context, safeRecipes, maxCount, recentRecipeIds, targetDate, mealType);
}
```

**Allergen Filtering Logic:**
```csharp
private async Task<List<Recipe>> FilterAllergens(List<Recipe> recipes, CustomerContext context)
{
    if (!context.Allergies.Any())
        return recipes;

    // Get allergen ingredient IDs
    var allergenNames = context.Allergies.Select(a => a.AllergyName).ToHashSet();
    var allIngredients = await _unitOfWork.Ingredients.GetAllAsync();
    var allergenIngredientIds = allIngredients
        .Where(i => i.IsAllergen && allergenNames.Contains(i.IngredientName))
        .Select(i => i.Id)
        .ToHashSet();

    // Filter recipes that don't contain allergen ingredients
    var safeRecipes = new List<Recipe>();
    foreach (var recipe in recipes)
    {
        var recipeIngredients = await _unitOfWork.RecipeIngredients.GetByRecipeIdAsync(recipe.Id);
        var hasAllergen = recipeIngredients.Any(ri => allergenIngredientIds.Contains(ri.IngredientId));
        
        if (!hasAllergen)
            safeRecipes.Add(recipe);
    }

    return safeRecipes;
}
```

**Key Actions:**
1. Retrieves all recipes from database
2. **Filters out recipes containing customer's allergens** (critical safety step)
3. Gets recipes used in last 3 days to avoid repetition
4. Verifies AI service is enabled and available
5. Calls LLM service to generate recommendations (Step 6)

---

### Step 6: OpenAIRecommendationService Communicates with GPT

**File:** `src/MealPrepService.BusinessLogicLayer/Services/OpenAIRecommendationService.cs`

**Method:** `GenerateRecommendationsAsync(CustomerContext context, List<Recipe> candidateRecipes, int maxRecommendations, List<Guid> recentRecipeIds, DateTime targetDate, string mealType)`

```csharp
public async Task<AIRecommendationResponse> GenerateRecommendationsAsync(
    CustomerContext context, List<Recipe> candidateRecipes, int maxRecommendations,
    List<Guid> recentRecipeIds, DateTime targetDate, string mealType)
{
    // 1. Build diversity-aware prompt
    var prompt = BuildDiversityAwarePrompt(
        context, candidateRecipes, maxRecommendations, 
        recentRecipeIds, targetDate, mealType);

    // 2. Call OpenAI API
    var response = await CallOpenAIAsync(prompt);
    
    // 3. Parse JSON response
    return ParseRecommendationResponse(response, candidateRecipes);
}
```

**Prompt Structure (BuildDiversityAwarePrompt):**

The prompt sent to GPT includes:

1. **System Role:** "You are a professional nutritionist and meal planning expert"

2. **Customer Profile:**
   - Name
   - Calorie goal (from HealthProfile.CalorieGoal)
   - Dietary restrictions (from HealthProfile.DietaryRestrictions)
   - Health notes (from HealthProfile.HealthNotes)
   - Food preferences (from HealthProfile.FoodPreferences)
   - Allergies list with ⚠️ CRITICAL warning

3. **Diversity Requirements:**
   - Target meal type and date
   - 🚫 FORBIDDEN RECIPES list (used in last 3 days)
   - ✅ VARIETY RULES:
     - Different cooking methods
     - Varied protein sources
     - Different cuisines
     - Mix ingredient combinations

4. **Available Recipes:**
   - Filtered list (excluding recent recipes)
   - Randomized order
   - Limited to 50 recipes (to avoid token limits)
   - Each recipe includes:
     - Name
     - Nutrition (calories, protein, carbs, fat)
     - Instructions preview

5. **Task Instructions:**
   - Select N recipes for the meal type
   - Must NOT be in forbidden list
   - Must match dietary needs
   - Must provide variety
   - Must be appropriate for meal type
   - Must offer nutritional balance

6. **Response Format (JSON):**
```json
{
  "recommendations": [
    {
      "recipeName": "Recipe Name",
      "confidenceScore": 0.95,
      "reasoning": "Why this recipe is perfect and adds variety",
      "matchedCriteria": ["criterion1", "criterion2"],
      "varietyFactors": ["cooking method", "protein type", "cuisine style"]
    }
  ],
  "overallReasoning": "Summary of variety and nutritional goals"
}
```

**OpenAI API Call:**

```csharp
private async Task<string> CallOpenAIAsync(string prompt)
{
    var requestBody = new
    {
        model = _model, // "gpt-4o-mini" or "gpt-4"
        messages = new[]
        {
            new { role = "system", content = "You are a professional nutritionist and meal planning expert." },
            new { role = "user", content = prompt }
        },
        temperature = 0.7,
        max_tokens = 1500
    };

    var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, requestBody);
    
    var responseContent = await response.Content.ReadAsStringAsync();
    var jsonResponse = JsonDocument.Parse(responseContent);
    
    var content = jsonResponse.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();

    return content ?? string.Empty;
}
```

**Configuration (appsettings.json):**
```json
{
  "AI": {
    "UseRealAI": true,
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4o-mini",
      "Endpoint": "https://api.openai.com/v1/chat/completions"
    }
  }
}
```

**Response Parsing:**

```csharp
private AIRecommendationResponse ParseRecommendationResponse(
    string aiResponse, List<Recipe> candidateRecipes)
{
    // Extract JSON from response (handles markdown formatting)
    var jsonStart = aiResponse.IndexOf('{');
    var jsonEnd = aiResponse.LastIndexOf('}') + 1;
    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);
    
    var parsed = JsonDocument.Parse(jsonContent);
    var result = new AIRecommendationResponse();
    
    // Parse recommendations array
    foreach (var rec in parsed.RootElement.GetProperty("recommendations").EnumerateArray())
    {
        var recipeName = rec.GetProperty("recipeName").GetString();
        var matchingRecipe = candidateRecipes.FirstOrDefault(r => 
            r.RecipeName.Equals(recipeName, StringComparison.OrdinalIgnoreCase));
        
        if (matchingRecipe != null)
        {
            result.RecommendedRecipes.Add(new AIRecommendedRecipe
            {
                RecipeId = matchingRecipe.Id,
                RecipeName = matchingRecipe.RecipeName,
                ConfidenceScore = rec.GetProperty("confidenceScore").GetDouble(),
                Reasoning = rec.GetProperty("reasoning").GetString() ?? "",
                MatchedCriteria = rec.GetProperty("matchedCriteria")
                    .EnumerateArray()
                    .Select(c => c.GetString() ?? "")
                    .ToList()
            });
        }
    }
    
    return result;
}
```

---

### Step 7: Create Meals from AI Recommendations

**File:** `src/MealPrepService.BusinessLogicLayer/Services/MealPlanService.cs`

**Method:** `CreateMealsFromRecommendations(MealPlan mealPlan, IEnumerable<MealRecommendation> recommendations)`

```csharp
private async Task CreateMealsFromRecommendations(
    MealPlan mealPlan, IEnumerable<MealRecommendation> recommendations)
{
    foreach (var recommendation in recommendations)
    {
        // 1. Create meal entity
        var meal = new Meal
        {
            Id = Guid.NewGuid(),
            PlanId = mealPlan.Id,
            MealType = recommendation.MealType.ToLower(),
            ServeDate = recommendation.Date,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Meals.AddAsync(meal);
        await _unitOfWork.SaveChangesAsync(); // Save to get meal ID

        // 2. Link recipes to meal via MealRecipe junction table
        foreach (var recipeId in recommendation.RecommendedRecipeIds)
        {
            var mealRecipe = new MealRecipe
            {
                MealId = meal.Id,
                RecipeId = recipeId
            };

            await _unitOfWork.MealRecipes.AddAsync(mealRecipe);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
```

**Database Structure:**
```
MealPlan (1) ──→ (N) Meal (N) ──→ (N) Recipe
                                  (via MealRecipe junction table)
```

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User clicks "Generate AI Meal Plan"                          │
│    Input: Start Date, End Date, Plan Name                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. MealPlanController.GenerateAI()                              │
│    - Validates input                                             │
│    - Gets current user ID                                        │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. MealPlanService.GenerateAiMealPlanAsync()                    │
│    - Checks meal plan limit (max 5)                             │
│    - Creates empty MealPlan entity                              │
│    - Saves to database                                           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. AIRecommendationService.GetMealPlanRecommendationsAsync()    │
│    - Checks if AI is enabled                                     │
│    - Gets AI configuration                                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. CustomerProfileAnalyzer.AnalyzeCustomerAsync()               │
│    - Loads customer account                                      │
│    - Loads health profile                                        │
│    - Loads allergies                                             │
│    - Loads order history                                         │
│    Output: CustomerContext                                       │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Loop: For each day and meal type (Breakfast, Lunch, Dinner)  │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 7. AIRecommendationEngine.GenerateRecommendationsAsync()        │
│    - Gets all recipes from database                              │
│    - Filters out allergen recipes                                │
│    - Gets recent recipes (last 3 days)                           │
│    - Verifies AI service availability                            │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 8. OpenAIRecommendationService.GenerateRecommendationsAsync()   │
│    - Builds diversity-aware prompt                               │
│    - Includes customer profile                                   │
│    - Includes forbidden recipes                                  │
│    - Includes safe recipe list                                   │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 9. OpenAI GPT API Call                                           │
│    POST https://api.openai.com/v1/chat/completions              │
│    Model: gpt-4o-mini                                            │
│    Temperature: 0.7                                              │
│    Max Tokens: 1500                                              │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 10. GPT Response (JSON)                                          │
│     - Recipe recommendations                                     │
│     - Confidence scores                                          │
│     - Reasoning explanations                                     │
│     - Matched criteria                                           │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 11. Parse and Map Response                                       │
│     - Extract recipe names                                       │
│     - Match to Recipe IDs                                        │
│     - Create MealRecommendation objects                          │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 12. End Loop - Collect all recommendations                       │
│     Output: List<MealRecommendation> (one per meal)             │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 13. MealPlanService.CreateMealsFromRecommendations()            │
│     - Creates Meal entities                                      │
│     - Links recipes via MealRecipe junction table                │
│     - Saves to database                                          │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 14. Return Complete Meal Plan                                    │
│     - Loads full plan with meals and recipes                     │
│     - Maps to MealPlanDto                                        │
│     - Returns to controller                                      │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ 15. Redirect to Meal Plan Details Page                          │
│     User sees complete AI-generated meal plan                    │
└─────────────────────────────────────────────────────────────────┘
```

## Key Features

### 1. Allergen Safety (Critical)

The system **always filters out recipes containing customer allergens** before sending to AI:

```csharp
// Step 1: Get customer allergies
var allergenNames = context.Allergies.Select(a => a.AllergyName).ToHashSet();

// Step 2: Find ingredients marked as allergens
var allergenIngredientIds = allIngredients
    .Where(i => i.IsAllergen && allergenNames.Contains(i.IngredientName))
    .Select(i => i.Id)
    .ToHashSet();

// Step 3: Filter recipes
foreach (var recipe in recipes)
{
    var recipeIngredients = await _unitOfWork.RecipeIngredients.GetByRecipeIdAsync(recipe.Id);
    var hasAllergen = recipeIngredients.Any(ri => allergenIngredientIds.Contains(ri.IngredientId));
    
    if (!hasAllergen)
        safeRecipes.Add(recipe);
}
```

**Safety Layers:**
1. Database-level filtering (before AI)
2. AI prompt includes allergen warnings
3. Only safe recipes are sent to GPT

### 2. Diversity and Variety

The system avoids recipe repetition using:

**Recent Recipe Tracking:**
```csharp
// Get recipes used in last 3 days
var threeDaysAgo = targetDate.AddDays(-3);
var recentRecipeIds = await _unitOfWork.MealPlans.GetRecentRecipeIdsAsync(
    context.Customer.Id, threeDaysAgo, targetDate.AddDays(-1));
```

**AI Prompt Instructions:**
- 🚫 FORBIDDEN RECIPES list (explicitly tells GPT to avoid these)
- ✅ VARIETY RULES (different cooking methods, proteins, cuisines)
- Randomized recipe order (prevents bias toward first recipes)

### 3. Personalization

AI considers multiple customer factors:

**From HealthProfile:**
- `CalorieGoal` - Daily calorie target (e.g., 2000 cal/day)
- `DietaryRestri