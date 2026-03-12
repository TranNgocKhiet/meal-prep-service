using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.Data;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace MealPreparationService.Business.Services;

public interface IAIMealPlanService
{
    Task<MealPlanDto> GenerateAIMealPlanAsync(CreateMealPlanDto dto, string userId);
}

public class AIMealPlanService : IAIMealPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AIMealPlanService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMealPlanService _mealPlanService;

    public AIMealPlanService(
        IUnitOfWork unitOfWork,
        ApplicationDbContext context,
        ILogger<AIMealPlanService> logger,
        IConfiguration configuration,
        IMealPlanService mealPlanService)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _mealPlanService = mealPlanService;
    }

    public async Task<MealPlanDto> GenerateAIMealPlanAsync(CreateMealPlanDto dto, string userId)
    {
        _logger.LogInformation("=== STARTING AI MEAL PLAN GENERATION ===");
        
        // Apply defaults for null values
        var durationDays = dto.DurationDays ?? 1;
        var startDate = dto.StartDate ?? DateTime.Today;
        
        _logger.LogInformation("User: {UserId}, Duration: {Days} days, Start: {StartDate}", 
            userId, durationDays, startDate.ToString("yyyy-MM-dd"));

        // Get user's health profile
        var healthProfile = await _unitOfWork.HealthProfiles.GetByAccountIdAsync(userId);
        _logger.LogInformation("Health profile loaded: {HasProfile}", healthProfile != null);
        
        // Get user's fridge items
        var fridgeItems = await _unitOfWork.FridgeItems.GetByAccountIdAsync(userId);
        _logger.LogInformation("Fridge items loaded: {Count} items", fridgeItems?.Count() ?? 0);
        
        // Get all recipes with ingredients
        var recipes = await _unitOfWork.Recipes.GetAllWithIngredientsAsync();
        _logger.LogInformation("Recipes loaded: {Count} recipes", recipes?.Count() ?? 0);
        
        // Build context for AI
        _logger.LogInformation("Building AI context...");
        var context = await BuildAIContextAsync(dto, healthProfile, fridgeItems, recipes);
        
        // Call OpenAI API
        _logger.LogInformation("Calling OpenAI API...");
        var selectedRecipes = await CallOpenAIAsync(context, durationDays);
        _logger.LogInformation("OpenAI returned recipes for {DayCount} days with {TotalRecipes} total recipes", 
            selectedRecipes.Count, selectedRecipes.Sum(d => d.Value.Count));
        
        // Create meal plan with AI-generated recipes
        _logger.LogInformation("Creating meal plan with recipes...");
        var mealPlan = await CreateMealPlanWithRecipesAsync(dto, userId, selectedRecipes);
        
        _logger.LogInformation("=== AI MEAL PLAN GENERATION COMPLETED ===");
        return mealPlan;
    }

    private async Task<string> BuildAIContextAsync(
        CreateMealPlanDto dto,
        HealthProfile? healthProfile,
        List<FridgeItem> fridgeItems,
        List<Recipe> recipes)
    {
        var context = new
        {
            UserProfile = new
            {
                Age = dto.Age ?? healthProfile?.Age,
                Weight = dto.Weight ?? healthProfile?.Weight,
                Height = dto.Height ?? healthProfile?.Height,
                Gender = dto.Gender ?? healthProfile?.Gender,
                HealthNote = dto.HealthNote ?? healthProfile?.HealthNotes,
                CaloriesGoal = dto.CaloriesGoal ?? healthProfile?.CalorieGoal
            },
            Allergies = await GetAllergiesAsync(dto.Allergies),
            LikedIngredients = await GetIngredientsAsync(dto.LikedIngredients),
            DislikedIngredients = await GetIngredientsAsync(dto.DislikedIngredients),
            AllergyIngredients = await GetIngredientsAsync(dto.AllergyIngredients),
            AvailableIngredients = fridgeItems.Select(fi => new
            {
                Name = fi.Ingredient.Name,
                Quantity = fi.CurrentAmount,
                Unit = fi.Ingredient.Unit,
                ExpiryDate = fi.ExpiryDate
            }).ToList(),
            AvailableRecipes = recipes.Select(r => new
            {
                Id = r.Id,
                Name = r.RecipeName,
                Instructions = r.Instructions,
                Ingredients = r.RecipeIngredients.Select(ri => new
                {
                    Name = ri.Ingredient.Name,
                    Amount = ri.Amount,
                    Unit = ri.Ingredient.Unit,
                    Allergies = ri.Ingredient.IngredientAllergies.Select(ia => ia.Allergy.Name).ToList()
                }).ToList()
            }).ToList(),
            Requirements = new
            {
                DurationDays = dto.DurationDays,
                MealsPerDay = 3, // Breakfast, Lunch, Dinner
                AvoidAllergies = true,
                PreferAvailableIngredients = true,
                RespectPreferences = true
            }
        };

        return JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
    }

    private async Task<List<string>> GetAllergiesAsync(List<string>? allergyIds)
    {
        if (allergyIds == null || !allergyIds.Any())
            return new List<string>();

        var allergies = new List<string>();
        foreach (var id in allergyIds)
        {
            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (allergy != null)
                allergies.Add(allergy.Name);
        }
        return allergies;
    }

    private async Task<List<string>> GetIngredientsAsync(List<string>? ingredientIds)
    {
        if (ingredientIds == null || !ingredientIds.Any())
            return new List<string>();

        var ingredients = new List<string>();
        foreach (var id in ingredientIds)
        {
            var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(id);
            if (ingredient != null)
                ingredients.Add(ingredient.Name);
        }
        return ingredients;
    }

    private async Task<Dictionary<int, List<RecipeSelection>>> CallOpenAIAsync(string context, int durationDays)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured");
        }

        var client = new ChatClient("gpt-4o-mini", apiKey);

        var systemPrompt = @"You are a professional nutritionist and meal planner. Your task is to create a personalized meal plan based on the user's health profile, dietary restrictions, preferences, and available ingredients.

CRITICAL RULES:
1. NEVER include recipes with ingredients that cause allergies for the user
2. Check each recipe's ingredients against the user's allergy list
3. Prefer recipes that use available ingredients from the user's fridge
4. Respect user's liked and disliked ingredients
5. Ensure nutritional balance across all meals
6. Consider the user's calorie goals
7. Create variety - don't repeat the same recipe too often
8. IMPORTANT: Copy recipeId values EXACTLY as provided, including all leading zeros and dashes. Do not modify or truncate GUIDs.
9. MANDATORY: You MUST provide EXACTLY 3 meals for EACH day (Breakfast, Lunch, Dinner). Never skip a meal or day.
10. MANDATORY: Each meal MUST have at least 1 recipe. Never create an empty meal.

Return ONLY a JSON object in this exact format:
{
  ""mealPlan"": {
    ""1"": [
      { ""recipeId"": ""recipe-id-1"", ""recipeName"": ""Recipe Name"", ""mealType"": ""Breakfast"" },
      { ""recipeId"": ""recipe-id-2"", ""recipeName"": ""Recipe Name"", ""mealType"": ""Lunch"" },
      { ""recipeId"": ""recipe-id-3"", ""recipeName"": ""Recipe Name"", ""mealType"": ""Dinner"" }
    ],
    ""2"": [
      { ""recipeId"": ""recipe-id-4"", ""recipeName"": ""Recipe Name"", ""mealType"": ""Breakfast"" },
      { ""recipeId"": ""recipe-id-5"", ""recipeName"": ""Recipe Name"", ""mealType"": ""Lunch"" },
      { ""recipeId"": ""recipe-id-6"", ""recipeName"": ""Recipe Name"", ""mealType"": ""Dinner"" }
    ]
  },
  ""reasoning"": ""Brief explanation of your choices""
}

IMPORTANT: The mealPlan object must have entries for ALL days from 1 to N, and each day must have EXACTLY 3 meals.";

        var userPrompt = $@"Create a {durationDays}-day meal plan with 3 meals per day (Breakfast, Lunch, Dinner).

User Context:
{context}

Select recipes from the AvailableRecipes list. Ensure each recipe is safe for the user (no allergy ingredients).";

        try
        {
            var completion = await client.CompleteChatAsync(
                new UserChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            );

            var responseText = completion.Value.Content[0].Text;
            _logger.LogInformation("OpenAI Response: {Response}", responseText);

            // Strip markdown code blocks if present
            responseText = responseText.Trim();
            if (responseText.StartsWith("```json"))
            {
                responseText = responseText.Substring(7); // Remove ```json
            }
            else if (responseText.StartsWith("```"))
            {
                responseText = responseText.Substring(3); // Remove ```
            }
            
            if (responseText.EndsWith("```"))
            {
                responseText = responseText.Substring(0, responseText.Length - 3); // Remove trailing ```
            }
            
            responseText = responseText.Trim();

            // Parse the JSON response
            var aiResponse = JsonSerializer.Deserialize<AIResponse>(responseText);
            if (aiResponse?.MealPlan == null)
            {
                throw new InvalidOperationException("Invalid AI response format");
            }

            // Convert string keys to int keys
            var result = new Dictionary<int, List<RecipeSelection>>();
            foreach (var kvp in aiResponse.MealPlan)
            {
                if (int.TryParse(kvp.Key, out int dayNumber))
                {
                    result[dayNumber] = kvp.Value;
                }
                else
                {
                    _logger.LogWarning("Invalid day number in AI response: {Key}", kvp.Key);
                }
            }

            // Validate that we have all days and all meals
            var missingDays = new List<int>();
            var incompleteDays = new List<string>();
            
            for (int day = 1; day <= durationDays; day++)
            {
                if (!result.ContainsKey(day))
                {
                    missingDays.Add(day);
                    _logger.LogError("AI response missing day {Day}", day);
                }
                else
                {
                    var meals = result[day];
                    var mealTypes = meals.Select(m => m.MealType).ToList();
                    
                    if (!mealTypes.Contains("Breakfast", StringComparer.OrdinalIgnoreCase))
                        incompleteDays.Add($"Day {day}: missing Breakfast");
                    if (!mealTypes.Contains("Lunch", StringComparer.OrdinalIgnoreCase))
                        incompleteDays.Add($"Day {day}: missing Lunch");
                    if (!mealTypes.Contains("Dinner", StringComparer.OrdinalIgnoreCase))
                        incompleteDays.Add($"Day {day}: missing Dinner");
                    
                    if (meals.Count < 3)
                    {
                        _logger.LogWarning("Day {Day} has only {Count} meals instead of 3", day, meals.Count);
                    }
                }
            }
            
            if (missingDays.Any() || incompleteDays.Any())
            {
                var errorMsg = "AI response validation failed. ";
                if (missingDays.Any())
                    errorMsg += $"Missing days: {string.Join(", ", missingDays)}. ";
                if (incompleteDays.Any())
                    errorMsg += $"Incomplete meals: {string.Join("; ", incompleteDays)}.";
                
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg + " Please try again.");
            }

            _logger.LogInformation("Parsed {DayCount} days with {RecipeCount} total recipes", 
                result.Count, result.Sum(d => d.Value.Count));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            throw new InvalidOperationException("Failed to generate AI meal plan", ex);
        }
    }

    private async Task<MealPlanDto> CreateMealPlanWithRecipesAsync(
        CreateMealPlanDto dto,
        string userId,
        Dictionary<int, List<RecipeSelection>> selectedRecipes)
    {
        _logger.LogInformation("Starting meal plan creation with {DayCount} days", selectedRecipes.Count);
        
        // Apply defaults for null values
        var durationDays = dto.DurationDays ?? 1;
        var startDate = dto.StartDate ?? DateTime.Today;
        
        // Create the meal plan first
        var mealPlan = new MealPlan
        {
            Id = Guid.NewGuid().ToString(),
            AccountId = userId,
            PlanName = dto.Name,
            StartDate = startDate,
            EndDate = startDate.AddDays(durationDays - 1), // Fixed: Duration of 1 day means start and end are the same day
            IsAiGenerated = true,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Age = dto.Age,
            Weight = dto.Weight,
            Height = dto.Height,
            Gender = dto.Gender,
            HealthNote = dto.HealthNote,
            CaloriesGoal = dto.CaloriesGoal
        };

        _context.MealPlans.Add(mealPlan);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Meal plan created with ID: {MealPlanId}", mealPlan.Id);

        // Get all meal types once
        var mealTypes = await _context.MealTypes.ToListAsync();
        _logger.LogInformation("Available meal types: {MealTypes}", 
            string.Join(", ", mealTypes.Select(mt => $"{mt.TypeName}(ID:{mt.Id})")));

        // Create meals for each day
        int totalMealsCreated = 0;
        int totalRecipesAdded = 0;
        
        foreach (var day in selectedRecipes.OrderBy(d => d.Key))
        {
            var dayNumber = day.Key;
            var serverDate = startDate.AddDays(dayNumber - 1);

            _logger.LogInformation("=== Processing Day {DayNumber} ({Date}) with {MealCount} meals ===", 
                dayNumber, serverDate.ToString("yyyy-MM-dd"), day.Value.Count);

            foreach (var recipeSelection in day.Value)
            {
                try
                {
                    _logger.LogInformation("  Creating meal: {MealType} - Recipe: {RecipeName} (ID: {RecipeId})", 
                        recipeSelection.MealType, recipeSelection.RecipeName, recipeSelection.RecipeId);

                    // Validate recipe exists and load it with nutrients
                    var recipe = await _context.Recipes
                        .Include(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
                                .ThenInclude(i => i.IngredientNutrients)
                                    .ThenInclude(inu => inu.Nutrient)
                        .FirstOrDefaultAsync(r => r.Id == recipeSelection.RecipeId);
                    
                    if (recipe == null)
                    {
                        _logger.LogWarning("  ⚠ Recipe ID {RecipeId} not found in database. Finding fallback recipe...", recipeSelection.RecipeId);
                        
                        // Get all recipe IDs first, then pick one randomly in memory
                        var allRecipeIds = await _context.Recipes.Select(r => r.Id).ToListAsync();
                        
                        if (!allRecipeIds.Any())
                        {
                            _logger.LogError("  ERROR: No recipes available in database for fallback!");
                            continue;
                        }
                        
                        // Pick a random recipe ID
                        var random = new Random();
                        var randomRecipeId = allRecipeIds[random.Next(allRecipeIds.Count)];
                        
                        // Load the fallback recipe with all includes
                        var fallbackRecipe = await _context.Recipes
                            .Include(r => r.RecipeIngredients)
                                .ThenInclude(ri => ri.Ingredient)
                                    .ThenInclude(i => i.IngredientNutrients)
                                        .ThenInclude(inu => inu.Nutrient)
                            .FirstOrDefaultAsync(r => r.Id == randomRecipeId);
                        
                        if (fallbackRecipe == null)
                        {
                            _logger.LogError("  ERROR: Failed to load fallback recipe!");
                            continue;
                        }
                        
                        recipe = fallbackRecipe;
                        recipeSelection.RecipeId = recipe.Id; // Update to use the fallback recipe ID
                        _logger.LogInformation("  ✓ Using fallback recipe: {RecipeName} (ID: {RecipeId})", recipe.RecipeName, recipe.Id);
                    }

                    // Find MealType
                    var mealType = mealTypes.FirstOrDefault(mt => 
                        mt.TypeName.Equals(recipeSelection.MealType, StringComparison.OrdinalIgnoreCase));
                    
                    if (mealType == null)
                    {
                        _logger.LogError("  ERROR: MealType '{MealType}' not found in database!", recipeSelection.MealType);
                        continue;
                    }

                    // Calculate nutritional values from recipe ingredients
                    decimal totalCalories = 0;
                    decimal totalProtein = 0;
                    decimal totalFat = 0;
                    decimal totalCarbs = 0;

                    foreach (var recipeIngredient in recipe.RecipeIngredients)
                    {
                        var ingredient = recipeIngredient.Ingredient;
                        var amount = recipeIngredient.Amount;

                        foreach (var ingredientNutrient in ingredient.IngredientNutrients)
                        {
                            var nutrientName = ingredientNutrient.Nutrient.Name.ToLower();
                            // AmountPer100 is per 100g, so calculate based on actual amount
                            var nutrientAmount = (ingredientNutrient.AmountPer100 / 100) * amount;

                            if (nutrientName.Contains("calorie") || nutrientName.Contains("energy"))
                            {
                                totalCalories += nutrientAmount;
                            }
                            else if (nutrientName.Contains("protein"))
                            {
                                totalProtein += nutrientAmount;
                            }
                            else if (nutrientName.Contains("fat") && !nutrientName.Contains("saturated"))
                            {
                                totalFat += nutrientAmount;
                            }
                            else if (nutrientName.Contains("carb"))
                            {
                                totalCarbs += nutrientAmount;
                            }
                        }
                    }

                    // Create meal with calculated nutrition
                    var meal = new Meal
                    {
                        Id = Guid.NewGuid().ToString(),
                        PlanId = mealPlan.Id,
                        MealTypeId = mealType.Id,
                        ServerDate = serverDate,
                        MealFinished = false,
                        TotalCalories = totalCalories,
                        ProteinG = totalProtein,
                        FatG = totalFat,
                        CarbsG = totalCarbs,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Meals.Add(meal);
                    await _context.SaveChangesAsync();
                    totalMealsCreated++;
                    _logger.LogInformation("  ✓ Meal created: ID={MealId}, Type={MealType}(ID:{MealTypeId}), Calories={Calories}, Protein={Protein}g, Fat={Fat}g, Carbs={Carbs}g", 
                        meal.Id, mealType.TypeName, mealType.Id, totalCalories, totalProtein, totalFat, totalCarbs);

                    // Add recipe to meal
                    var mealRecipe = new MealRecipe
                    {
                        Id = Guid.NewGuid().ToString(),
                        MealId = meal.Id,
                        RecipeId = recipeSelection.RecipeId
                    };

                    _context.MealRecipes.Add(mealRecipe);
                    await _context.SaveChangesAsync();
                    totalRecipesAdded++;
                    _logger.LogInformation("  ✓ MealRecipe created: ID={MealRecipeId}, MealId={MealId}, RecipeId={RecipeId}", 
                        mealRecipe.Id, meal.Id, recipeSelection.RecipeId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "  ERROR creating meal for {MealType} - {RecipeName}", 
                        recipeSelection.MealType, recipeSelection.RecipeName);
                    throw;
                }
            }
        }

        _logger.LogInformation("=== SUMMARY: Created {MealCount} meals with {RecipeCount} recipes for meal plan {MealPlanId} ===", 
            totalMealsCreated, totalRecipesAdded, mealPlan.Id);

        // Return the created meal plan
        return await _mealPlanService.GetMealPlanByIdAsync(mealPlan.Id, userId);
    }

    private class AIResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("mealPlan")]
        public Dictionary<string, List<RecipeSelection>> MealPlan { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;
    }

    private class RecipeSelection
    {
        [System.Text.Json.Serialization.JsonPropertyName("recipeId")]
        public string RecipeId { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("recipeName")]
        public string RecipeName { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("mealType")]
        public string MealType { get; set; } = string.Empty;
    }
}
