using MealPreparationService.Business.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service for OpenAI API integration with timeout, rate limiting, and error handling
/// </summary>
public class OpenAiIntegrationService : IOpenAiService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiIntegrationService> _logger;
    private readonly int _timeoutSeconds;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly ICacheService _cacheService;

    public OpenAiIntegrationService(
        IConfiguration configuration,
        ILogger<OpenAiIntegrationService> logger,
        ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
        
        var apiKey = configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI API key is not configured");
        
        var model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        _timeoutSeconds = int.TryParse(configuration["SystemConfiguration:OpenAiTimeoutSeconds"], out var timeout) ? timeout : 30;
        var maxRequestsPerMinute = int.TryParse(configuration["OpenAI:MaxRequestsPerMinute"], out var maxReq) ? maxReq : 10;
        
        _chatClient = new ChatClient(model, new ApiKeyCredential(apiKey));
        _rateLimiter = new SemaphoreSlim(maxRequestsPerMinute, maxRequestsPerMinute);
        
        _logger.LogInformation("OpenAI service initialized with model: {Model}, timeout: {Timeout}s", 
            model, _timeoutSeconds);
    }

    /// <summary>
    /// Generates AI meal plan suggestions based on user context
    /// </summary>
    public async Task<AiMealPlanResponseDto> GenerateMealPlanAsync(AiMealPlanPromptDto prompt)
    {
        try
        {
            // Generate cache key from prompt parameters
            var cacheKey = GenerateCacheKey("mealplan", prompt);
            
            // Check cache first
            var cachedResult = await _cacheService.GetAsync<AiMealPlanResponseDto>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Returning cached meal plan result");
                return cachedResult;
            }

            // Format prompt for meal plan generation
            var formattedPrompt = FormatMealPlanPrompt(prompt);
            
            _logger.LogInformation("Requesting AI meal plan generation for {Days} days", prompt.DurationDays);
            
            // Call OpenAI with timeout and rate limiting
            var response = await CallOpenAiWithTimeoutAsync(formattedPrompt);
            
            // Parse response
            var result = ParseMealPlanResponse(response, prompt.DurationDays);
            
            // Cache the result for 24 hours
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(24));
            
            _logger.LogInformation("Successfully generated AI meal plan");
            return result;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "OpenAI meal plan generation timed out after {Timeout}s", _timeoutSeconds);
            throw new InvalidOperationException(
                $"AI meal plan generation timed out after {_timeoutSeconds} seconds. Please try manual creation.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI meal plan");
            throw new InvalidOperationException(
                "Failed to generate AI meal plan. Please try manual creation.", ex);
        }
    }

    /// <summary>
    /// Calculates nutritional information for ingredients
    /// </summary>
    public async Task<NutrientDataDto> CalculateNutrientsAsync(NutrientPromptDto prompt)
    {
        try
        {
            // Generate cache key from prompt parameters
            var cacheKey = GenerateCacheKey("nutrients", prompt);
            
            // Check cache first
            var cachedResult = await _cacheService.GetAsync<NutrientDataDto>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Returning cached nutrient calculation result");
                return cachedResult;
            }

            // Format prompt for nutrient calculation
            var formattedPrompt = FormatNutrientPrompt(prompt);
            
            _logger.LogInformation("Requesting nutrient calculation for {Count} ingredients", 
                prompt.Ingredients.Count);
            
            // Call OpenAI with timeout and rate limiting
            var response = await CallOpenAiWithTimeoutAsync(formattedPrompt);
            
            // Parse response
            var result = ParseNutrientResponse(response);
            
            // Cache the result for 24 hours
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(24));
            
            _logger.LogInformation("Successfully calculated nutrients");
            return result;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "OpenAI nutrient calculation timed out after {Timeout}s", _timeoutSeconds);
            
            // Try to return cached data if available
            var cacheKey = GenerateCacheKey("nutrients", prompt);
            var cachedResult = await _cacheService.GetAsync<NutrientDataDto>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Returning cached nutrient data after timeout");
                return cachedResult;
            }
            
            throw new InvalidOperationException(
                $"Nutrient calculation timed out after {_timeoutSeconds} seconds and no cached data available.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating nutrients");
            throw new InvalidOperationException("Failed to calculate nutrients.", ex);
        }
    }

    /// <summary>
    /// Gets health advice based on context
    /// </summary>
    public async Task<string> GetHealthAdviceAsync(string context)
    {
        try
        {
            var prompt = $@"Based on the following nutritional information, provide brief health advice (2-3 sentences):

{context}

Provide practical, actionable health advice.";

            _logger.LogInformation("Requesting health advice");
            
            var response = await CallOpenAiWithTimeoutAsync(prompt);
            
            _logger.LogInformation("Successfully generated health advice");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health advice");
            return "Unable to generate health advice at this time.";
        }
    }

    /// <summary>
    /// Calls OpenAI API with timeout and rate limiting
    /// Validates: Requirements 21.3
    /// </summary>
    private async Task<string> CallOpenAiWithTimeoutAsync(string prompt)
    {
        // Apply rate limiting
        await _rateLimiter.WaitAsync();
        
        try
        {
            var requestStartTime = DateTime.UtcNow;
            
            // Log external API call (sanitized)
            _logger.LogInformation(
                "OpenAI API call started | Timestamp: {Timestamp} | Prompt length: {PromptLength} characters",
                requestStartTime, prompt.Length);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful nutrition and meal planning assistant. Always respond with valid JSON when requested."),
                new UserChatMessage(prompt)
            };

            var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cts.Token);
            
            var response = completion.Value.Content[0].Text;
            var requestEndTime = DateTime.UtcNow;
            var duration = (requestEndTime - requestStartTime).TotalMilliseconds;
            
            // Log successful API call with response details
            _logger.LogInformation(
                "OpenAI API call completed | Duration: {Duration}ms | Response length: {ResponseLength} characters | Timestamp: {Timestamp}",
                duration, response.Length, requestEndTime);
            
            _logger.LogDebug("OpenAI API response preview: {ResponsePreview}...", 
                response.Length > 200 ? response.Substring(0, 200) : response);
            
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OpenAI API call timed out after {Timeout}s", _timeoutSeconds);
            throw new TimeoutException($"OpenAI API call timed out after {_timeoutSeconds} seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI API call failed | Error: {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            // Release rate limiter after a delay to maintain rate limit
            _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => _rateLimiter.Release());
        }
    }

    /// <summary>
    /// Formats prompt for meal plan generation
    /// </summary>
    private string FormatMealPlanPrompt(AiMealPlanPromptDto prompt)
    {
        // Sort fridge items by expiry date (nearest first)
        var sortedFridgeItems = prompt.FridgeContents
            .OrderBy(f => f.ExpiryDate)
            .Take(20) // Limit to top 20 items to keep prompt manageable
            .ToList();

        var fridgeItemsText = string.Join("\n", sortedFridgeItems.Select(f => 
            $"- {f.Ingredient.Name}: {f.Quantity} {f.Unit} (expires: {f.ExpiryDate:yyyy-MM-dd})"));

        var recipesText = string.Join("\n", prompt.AvailableRecipes.Take(50).Select(r => 
            $"- ID: {r.Id}, Name: {r.RecipeName}, Category: {r.Category}, Prep Time: {r.PreparationTimeMinutes}min, Difficulty: {r.DifficultyLevel}"));

        var allergensText = prompt.UserAllergens.Any() 
            ? $"\nUser Allergies (MUST AVOID): {string.Join(", ", prompt.UserAllergens)}" 
            : "";

        return $@"Generate a {prompt.DurationDays}-day meal plan based on the following information:

Health Information: {prompt.HealthInformation}
Goals: {prompt.Goals}
{allergensText}

Available Fridge Items (prioritize items expiring soon):
{fridgeItemsText}

Available Recipes:
{recipesText}

Requirements:
1. Create meals for {prompt.DurationDays} days
2. Each day should have Breakfast (mealTypeId: 1), Lunch (mealTypeId: 2), and Dinner (mealTypeId: 3)
3. Prioritize recipes that use ingredients expiring soon
4. EXCLUDE any recipes containing allergens: {string.Join(", ", prompt.UserAllergens)}
5. Consider the user's health information and goals
6. Each meal can have 1-3 recipes

Respond with ONLY valid JSON in this exact format:
{{
  ""days"": [
    {{
      ""dayNumber"": 1,
      ""meals"": [
        {{
          ""mealTypeId"": 1,
          ""recipeIds"": [""recipe-id-1"", ""recipe-id-2""]
        }},
        {{
          ""mealTypeId"": 2,
          ""recipeIds"": [""recipe-id-3""]
        }},
        {{
          ""mealTypeId"": 3,
          ""recipeIds"": [""recipe-id-4""]
        }}
      ]
    }}
  ],
  ""reasoning"": ""Brief explanation of the meal plan choices""
}}";
    }

    /// <summary>
    /// Formats prompt for nutrient calculation
    /// </summary>
    private string FormatNutrientPrompt(NutrientPromptDto prompt)
    {
        var ingredientsText = string.Join("\n", prompt.Ingredients.Select(i => 
            $"- {i.IngredientName}: {i.Quantity} {i.Unit}"));

        return $@"Calculate the nutritional information for the following ingredients:

{ingredientsText}

Provide detailed nutritional analysis including:
- Total calories
- Total proteins (g)
- Total carbohydrates (g)
- Total fats (g)
- Key vitamins (Vitamin A, C, D, E, K, B12)
- Key minerals (Calcium, Iron, Magnesium, Potassium, Zinc)

Assume 1 serving and calculate per-serving values.

Respond with ONLY valid JSON in this exact format:
{{
  ""totalCalories"": 500.5,
  ""totalProteins"": 25.3,
  ""totalCarbohydrates"": 60.2,
  ""totalFats"": 15.8,
  ""vitamins"": {{
    ""Vitamin A"": 800.0,
    ""Vitamin C"": 45.0,
    ""Vitamin D"": 5.0,
    ""Vitamin E"": 10.0,
    ""Vitamin K"": 80.0,
    ""Vitamin B12"": 2.5
  }},
  ""minerals"": {{
    ""Calcium"": 300.0,
    ""Iron"": 8.0,
    ""Magnesium"": 100.0,
    ""Potassium"": 400.0,
    ""Zinc"": 5.0
  }},
  ""caloriesPerServing"": 500.5,
  ""servings"": 1
}}";
    }

    /// <summary>
    /// Parses meal plan response from OpenAI
    /// </summary>
    private AiMealPlanResponseDto ParseMealPlanResponse(string response, int expectedDays)
    {
        try
        {
            // Extract JSON from response (in case there's extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart == -1 || jsonEnd == -1)
            {
                throw new InvalidOperationException("No valid JSON found in response");
            }
            
            var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            
            var result = JsonSerializer.Deserialize<AiMealPlanResponseDto>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null || result.Days.Count == 0)
            {
                throw new InvalidOperationException("Failed to parse meal plan response");
            }

            // Validate the response
            if (result.Days.Count != expectedDays)
            {
                _logger.LogWarning("Expected {Expected} days but got {Actual}", expectedDays, result.Days.Count);
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse meal plan JSON response: {Response}", response);
            throw new InvalidOperationException("Failed to parse AI meal plan response", ex);
        }
    }

    /// <summary>
    /// Parses nutrient response from OpenAI
    /// </summary>
    private NutrientDataDto ParseNutrientResponse(string response)
    {
        try
        {
            // Extract JSON from response (in case there's extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart == -1 || jsonEnd == -1)
            {
                throw new InvalidOperationException("No valid JSON found in response");
            }
            
            var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            
            var result = JsonSerializer.Deserialize<NutrientDataDto>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to parse nutrient response");
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse nutrient JSON response: {Response}", response);
            throw new InvalidOperationException("Failed to parse nutrient calculation response", ex);
        }
    }

    /// <summary>
    /// Generates cache key from request parameters
    /// </summary>
    private string GenerateCacheKey(string prefix, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json));
        var hashString = Convert.ToBase64String(hash);
        return $"openai:{prefix}:{hashString}";
    }
}
