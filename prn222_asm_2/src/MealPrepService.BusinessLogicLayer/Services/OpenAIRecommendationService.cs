using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MealPrepService.BusinessLogicLayer.Services
{
    /// <summary>
    /// OpenAI-powered recommendation service using GPT models
    /// </summary>
    public class OpenAIRecommendationService : ILLMService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenAIRecommendationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiEndpoint;

        public OpenAIRecommendationService(
            IConfiguration configuration,
            ILogger<OpenAIRecommendationService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClientFactory.CreateClient();

            // Load configuration
            _apiKey = _configuration["AI:OpenAI:ApiKey"] 
                ?? throw new InvalidOperationException("OpenAI API key not configured");
            _model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o-mini";
            _apiEndpoint = _configuration["AI:OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";

            // Configure HTTP client
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MealPrepService/1.0");
        }

        public string GetModelName() => _model;

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                _logger.LogInformation("Checking OpenAI service availability with model: {Model}, endpoint: {Endpoint}", _model, _apiEndpoint);
                
                // Check if API key is configured
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogError("OpenAI API key is null or empty");
                    return false;
                }
                
                _logger.LogInformation("API key configured (length: {Length})", _apiKey.Length);

                // Simple health check - try to make a minimal API call
                var testRequest = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "user", content = "test" }
                    },
                    max_tokens = 5
                };

                _logger.LogInformation("Making test request to OpenAI API");
                var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, testRequest);
                
                _logger.LogInformation("OpenAI API response: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI service health check failed");
                return false;
            }
        }

        public async Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations)
        {
            try
            {
                _logger.LogInformation("Generating AI recommendations for customer {CustomerId} with {RecipeCount} candidates (max: {MaxRecommendations})",
                    context.Customer.Id,
                    candidateRecipes.Count,
                    maxRecommendations);

                var prompt = BuildRecommendationPrompt(context, candidateRecipes, maxRecommendations);
                var response = await CallOpenAIAsync(prompt);
                var result = ParseRecommendationResponse(response, candidateRecipes);

                _logger.LogInformation("AI recommendations generated successfully. Returned {RecommendationCount} recommendations",
                    result.RecommendedRecipes.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI recommendations");
                throw;
            }
        }

        public async Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations,
            List<Guid> recentRecipeIds,
            DateTime targetDate,
            string mealType)
        {
            try
            {
                _logger.LogInformation("Generating diversity-aware recommendations for customer {CustomerId}. MealType: {MealType}, TargetDate: {TargetDate}, Candidates: {CandidateCount}, RecentRecipes: {RecentCount}, Max: {MaxRecommendations}",
                    context.Customer.Id,
                    mealType,
                    targetDate,
                    candidateRecipes.Count,
                    recentRecipeIds.Count,
                    maxRecommendations);

                var prompt = BuildDiversityAwarePrompt(context, candidateRecipes, maxRecommendations, recentRecipeIds, targetDate, mealType);
                var response = await CallOpenAIAsync(prompt);
                var result = ParseRecommendationResponse(response, candidateRecipes);

                _logger.LogInformation("Diversity-aware recommendations generated successfully. Returned {RecommendationCount} recommendations",
                    result.RecommendedRecipes.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate diversity-aware AI recommendations");
                throw;
            }
        }

        public async Task<string> GenerateRecommendationReasoningAsync(Recipe recipe, CustomerContext context)
        {
            try
            {
                var prompt = BuildReasoningPrompt(recipe, context);
                return await CallOpenAIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate reasoning for recipe {RecipeId}", recipe.Id);
                return "This recipe matches your dietary preferences and nutritional goals.";
            }
        }

        public async Task<CustomMealNutritionAnalysis> AnalyzeCustomMealNutritionAsync(CustomMealNutritionRequest request)
        {
            try
            {
                _logger.LogInformation("Analyzing custom meal nutrition. DescriptionLength: {DescriptionLength}, IngredientCount: {IngredientCount}",
                    request.MealDescription?.Length ?? 0,
                    request.Ingredients?.Count ?? 0);

                var prompt = BuildCustomMealNutritionPrompt(request);
                var response = await CallOpenAIAsync(prompt);
                var result = ParseCustomMealNutritionResponse(response, request);

                _logger.LogInformation("Custom meal nutrition analysis completed. Calories: {Calories}, Protein: {Protein}g, Carbs: {Carbs}g, Fat: {Fat}g",
                    result.TotalCalories,
                    result.ProteinG,
                    result.CarbsG,
                    result.FatG);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze custom meal nutrition");
                return new CustomMealNutritionAnalysis
                {
                    MealSummary = request.MealDescription,
                    OverallNote = "Unable to analyze this meal right now. Please try again later."
                };
            }
        }

        private string BuildDiversityAwarePrompt(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations,
            List<Guid> recentRecipeIds,
            DateTime targetDate,
            string mealType)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("You are a professional nutritionist and meal planning expert. Create diverse, varied meal recommendations that avoid repetition.");
            sb.AppendLine();
            
            // Customer Profile
            sb.AppendLine("## Customer Profile");
            sb.AppendLine($"Name: {context.Customer.FullName}");
            
            if (context.HealthProfile != null)
            {
                sb.AppendLine($"Calorie Goal: {context.HealthProfile.CalorieGoal ?? 2000} cal/day");
                sb.AppendLine($"Dietary Restrictions: {context.HealthProfile.DietaryRestrictions ?? "None"}");
                sb.AppendLine($"Health Notes: {context.HealthProfile.HealthNotes ?? "General wellness"}");
                
                if (!string.IsNullOrWhiteSpace(context.HealthProfile.FoodPreferences))
                {
                    sb.AppendLine($"Food Preferences: {context.HealthProfile.FoodPreferences}");
                }
            }
            
            if (context.Allergies.Any())
            {
                sb.AppendLine($"Allergies: {string.Join(", ", context.Allergies.Select(a => a.AllergyName))}");
                sb.AppendLine("⚠️ CRITICAL: Never recommend recipes containing these allergens!");
            }
            
            sb.AppendLine();
            
            // Diversity Requirements
            sb.AppendLine("## DIVERSITY REQUIREMENTS (CRITICAL)");
            sb.AppendLine($"Target: {mealType} for {targetDate:yyyy-MM-dd}");
            
            if (recentRecipeIds.Any())
            {
                var recentRecipeNames = candidateRecipes
                    .Where(r => recentRecipeIds.Contains(r.Id))
                    .Select(r => r.RecipeName)
                    .ToList();
                
                if (recentRecipeNames.Any())
                {
                    sb.AppendLine("🚫 FORBIDDEN RECIPES (used in last 3 days):");
                    foreach (var recipeName in recentRecipeNames)
                    {
                        sb.AppendLine($"   - {recipeName}");
                    }
                    sb.AppendLine("❌ DO NOT recommend any of these recipes!");
                }
            }
            
            sb.AppendLine("✅ VARIETY RULES:");
            sb.AppendLine("   - Choose recipes with different cooking methods (grilled, baked, stir-fried, etc.)");
            sb.AppendLine("   - Vary protein sources (chicken, beef, fish, vegetarian, etc.)");
            sb.AppendLine("   - Include different cuisines when possible (Italian, Asian, Mexican, etc.)");
            sb.AppendLine("   - Mix different ingredient combinations");
            sb.AppendLine("   - Prioritize recipes that haven't been used recently");
            sb.AppendLine();
            
            // Available Recipes (filtered to exclude recent ones)
            var availableRecipes = candidateRecipes
                .Where(r => !recentRecipeIds.Contains(r.Id))
                .OrderBy(r => Guid.NewGuid()) // Randomize order
                .Take(50) // Limit to avoid token limits
                .ToList();
            
            sb.AppendLine("## Available Recipes (Filtered for diversity, randomized order)");
            sb.AppendLine($"Total available: {availableRecipes.Count} recipes (excluding {recentRecipeIds.Count} recent recipes)");
            sb.AppendLine();
            
            foreach (var recipe in availableRecipes)
            {
                sb.AppendLine($"- {recipe.RecipeName}");
                sb.AppendLine($"  Calories: {recipe.TotalCalories}, Protein: {recipe.ProteinG}g, Carbs: {recipe.CarbsG}g, Fat: {recipe.FatG}g");
                sb.AppendLine($"  Instructions: {(recipe.Instructions.Length > 100 ? recipe.Instructions.Substring(0, 100) + "..." : recipe.Instructions)}");
                sb.AppendLine();
            }
            
            // Instructions
            sb.AppendLine($"## Task");
            sb.AppendLine($"Select {maxRecommendations} recipe(s) for {mealType} that:");
            sb.AppendLine($"1. ❌ Are NOT in the forbidden list above");
            sb.AppendLine($"2. ✅ Match the customer's dietary needs and preferences");
            sb.AppendLine($"3. ✅ Provide variety in cooking method, protein, and cuisine");
            sb.AppendLine($"4. ✅ Are appropriate for {mealType}");
            sb.AppendLine($"5. ✅ Offer nutritional balance");
            sb.AppendLine();
            sb.AppendLine("Respond in JSON format:");
            sb.AppendLine(@"{
  ""recommendations"": [
    {
      ""recipeName"": ""Recipe Name"",
      ""confidenceScore"": 0.95,
      ""reasoning"": ""Why this recipe is perfect and adds variety"",
      ""matchedCriteria"": [""criterion1"", ""criterion2""],
      ""varietyFactors"": [""cooking method"", ""protein type"", ""cuisine style""]
    }
  ],
  ""overallReasoning"": ""Summary of how these recipes provide variety and meet nutritional goals""
}");

            return sb.ToString();
        }

        private string BuildCustomMealNutritionPrompt(CustomMealNutritionRequest request)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a professional nutritionist. Estimate nutrition for custom meals from user-provided ingredients.");
            sb.AppendLine("Use realistic nutrition estimations per ingredient amount and summarize totals.");
            sb.AppendLine();
            sb.AppendLine("## Meal Description");
            sb.AppendLine(request.MealDescription);
            sb.AppendLine();
            sb.AppendLine("## Ingredients");

            foreach (var ingredient in request.Ingredients)
            {
                sb.AppendLine($"- {ingredient.IngredientName}: {ingredient.Quantity} {ingredient.Unit}");
            }

            sb.AppendLine();
            sb.AppendLine("## Required Analysis");
            sb.AppendLine("1. Estimate total calories, protein(g), carbs(g), fat(g), fiber(g), sugar(g), sodium(mg).");
            sb.AppendLine("2. Detect potential conflicts between ingredients (digestion, nutrient absorption, high sodium/sugar/fat combinations, etc.).");
            sb.AppendLine("3. Suggest best way/time/portion to consume this meal.");
            sb.AppendLine("4. Keep advice practical and short.");
            sb.AppendLine();
            sb.AppendLine("Return JSON only in this format:");
            sb.AppendLine(@"{
  ""mealSummary"": ""Short meal summary"",
  ""nutrition"": {
    ""totalCalories"": 0,
    ""proteinG"": 0,
    ""carbsG"": 0,
    ""fatG"": 0,
    ""fiberG"": 0,
    ""sugarG"": 0,
    ""sodiumMg"": 0
  },
  ""ingredientConflicts"": [""Conflict note""],
  ""bestConsumptionAdvice"": ""Advice text"",
  ""overallNote"": ""Short overall nutrition note""
}");

            return sb.ToString();
        }

        private string BuildRecommendationPrompt(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("You are a professional nutritionist and meal planning expert. Analyze the customer profile and recommend the best meals.");
            sb.AppendLine();
            
            // Customer Profile
            sb.AppendLine("## Customer Profile");
            sb.AppendLine($"Name: {context.Customer.FullName}");
            
            if (context.HealthProfile != null)
            {
                sb.AppendLine($"Calorie Goal: {context.HealthProfile.CalorieGoal ?? 2000} cal/day");
                sb.AppendLine($"Dietary Restrictions: {context.HealthProfile.DietaryRestrictions ?? "None"}");
                sb.AppendLine($"Health Notes: {context.HealthProfile.HealthNotes ?? "General wellness"}");
            }
            
            if (context.Allergies.Any())
            {
                sb.AppendLine($"Allergies: {string.Join(", ", context.Allergies.Select(a => a.AllergyName))}");
                sb.AppendLine("⚠️ CRITICAL: Never recommend recipes containing these allergens!");
            }
            
            if (context.HealthProfile != null && !string.IsNullOrWhiteSpace(context.HealthProfile.FoodPreferences))
            {
                sb.AppendLine($"Food Preferences: {context.HealthProfile.FoodPreferences}");
            }
            
            sb.AppendLine();
            
            // Available Recipes
            sb.AppendLine("## Available Recipes (Already filtered for allergen safety)");
            foreach (var recipe in candidateRecipes.Take(50)) // Limit to avoid token limits
            {
                sb.AppendLine($"- {recipe.RecipeName}");
                sb.AppendLine($"  Calories: {recipe.TotalCalories}, Protein: {recipe.ProteinG}g, Carbs: {recipe.CarbsG}g, Fat: {recipe.FatG}g");
                sb.AppendLine($"  Instructions: {(recipe.Instructions.Length > 100 ? recipe.Instructions.Substring(0, 100) + "..." : recipe.Instructions)}");
                sb.AppendLine();
            }
            
            // Instructions
            sb.AppendLine($"## Task");
            sb.AppendLine($"Select the top {maxRecommendations} recipes that best match this customer's profile.");
            sb.AppendLine($"Consider: dietary restrictions, calorie goals, preferences, nutritional balance, and variety.");
            sb.AppendLine();
            sb.AppendLine("Respond in JSON format:");
            sb.AppendLine(@"{
  ""recommendations"": [
    {
      ""recipeName"": ""Recipe Name"",
      ""confidenceScore"": 0.95,
      ""reasoning"": ""Why this recipe is perfect for the customer"",
      ""matchedCriteria"": [""criterion1"", ""criterion2""]
    }
  ],
  ""overallReasoning"": ""Summary of the meal plan strategy""
}");

            return sb.ToString();
        }

        private string BuildReasoningPrompt(Recipe recipe, CustomerContext context)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"Explain in 1-2 sentences why '{recipe.RecipeName}' is a good choice for this customer:");
            sb.AppendLine();
            
            if (context.HealthProfile != null)
            {
                sb.AppendLine($"Customer Goals: {context.HealthProfile.HealthNotes ?? "General wellness"}");
                sb.AppendLine($"Dietary Restrictions: {context.HealthProfile.DietaryRestrictions ?? "None"}");
            }
            
            sb.AppendLine($"Recipe Nutrition: {recipe.TotalCalories} cal, {recipe.ProteinG}g protein, {recipe.CarbsG}g carbs, {recipe.FatG}g fat");
            
            sb.AppendLine();
            sb.AppendLine("Provide a friendly, personalized explanation (max 2 sentences):");

            return sb.ToString();
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            _logger.LogDebug("Calling OpenAI endpoint {Endpoint} using model {Model}. PromptLength: {PromptLength}",
                _apiEndpoint,
                _model,
                prompt.Length);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are a professional nutritionist and meal planning expert." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1500
            };

            var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new Exception($"OpenAI API error: {response.StatusCode}");
            }

            _logger.LogDebug("OpenAI API call successful. StatusCode: {StatusCode}", response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonDocument.Parse(responseContent);
            
            var content = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? string.Empty;
        }

        private AIRecommendationResponse ParseRecommendationResponse(string aiResponse, List<Recipe> candidateRecipes)
        {
            try
            {
                // Try to extract JSON from the response (AI might add markdown formatting)
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}') + 1;
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);
                    var parsed = JsonDocument.Parse(jsonContent);
                    
                    var result = new AIRecommendationResponse();
                    
                    if (parsed.RootElement.TryGetProperty("overallReasoning", out var reasoning))
                    {
                        result.OverallReasoning = reasoning.GetString() ?? string.Empty;
                    }
                    
                    if (parsed.RootElement.TryGetProperty("recommendations", out var recommendations))
                    {
                        foreach (var rec in recommendations.EnumerateArray())
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
                                    Reasoning = rec.GetProperty("reasoning").GetString() ?? string.Empty,
                                    MatchedCriteria = rec.GetProperty("matchedCriteria")
                                        .EnumerateArray()
                                        .Select(c => c.GetString() ?? string.Empty)
                                        .ToList()
                                });
                            }
                        }
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI response, using fallback");
            }
            
            // Fallback: return empty response
            return new AIRecommendationResponse
            {
                OverallReasoning = "Unable to parse AI recommendations"
            };
        }

        private CustomMealNutritionAnalysis ParseCustomMealNutritionResponse(
            string aiResponse,
            CustomMealNutritionRequest request)
        {
            try
            {
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);
                    var parsed = JsonDocument.Parse(jsonContent);
                    var root = parsed.RootElement;

                    var result = new CustomMealNutritionAnalysis
                    {
                        MealSummary = root.TryGetProperty("mealSummary", out var mealSummary)
                            ? mealSummary.GetString() ?? request.MealDescription
                            : request.MealDescription,
                        BestConsumptionAdvice = root.TryGetProperty("bestConsumptionAdvice", out var advice)
                            ? advice.GetString() ?? string.Empty
                            : string.Empty,
                        OverallNote = root.TryGetProperty("overallNote", out var overallNote)
                            ? overallNote.GetString() ?? string.Empty
                            : string.Empty
                    };

                    if (root.TryGetProperty("ingredientConflicts", out var conflicts) &&
                        conflicts.ValueKind == JsonValueKind.Array)
                    {
                        result.IngredientConflicts = conflicts
                            .EnumerateArray()
                            .Select(x => x.GetString())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Cast<string>()
                            .ToList();
                    }

                    if (root.TryGetProperty("nutrition", out var nutrition))
                    {
                        result.TotalCalories = GetNumberValue(nutrition, "totalCalories");
                        result.ProteinG = GetNumberValue(nutrition, "proteinG");
                        result.CarbsG = GetNumberValue(nutrition, "carbsG");
                        result.FatG = GetNumberValue(nutrition, "fatG");
                        result.FiberG = GetNumberValue(nutrition, "fiberG");
                        result.SugarG = GetNumberValue(nutrition, "sugarG");
                        result.SodiumMg = GetNumberValue(nutrition, "sodiumMg");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse custom nutrition response");
            }

            return new CustomMealNutritionAnalysis
            {
                MealSummary = request.MealDescription,
                OverallNote = "Unable to parse nutrition analysis response."
            };
        }

        private static double GetNumberValue(JsonElement parent, string propertyName)
        {
            if (!parent.TryGetProperty(propertyName, out var element))
            {
                return 0;
            }

            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.String when double.TryParse(element.GetString(), out var value) => value,
                _ => 0
            };
        }
    }
}
