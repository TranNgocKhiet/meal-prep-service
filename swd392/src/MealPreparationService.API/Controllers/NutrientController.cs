using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/nutrients")]
[Authorize(Roles = "Customer")]
public class NutrientController : ControllerBase
{
    private readonly INutrientCalculatorService _nutrientCalculatorService;
    private readonly IOpenAiService _openAiService;
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<NutrientController> _logger;

    public NutrientController(
        INutrientCalculatorService nutrientCalculatorService,
        IOpenAiService openAiService,
        IIngredientService ingredientService,
        ILogger<NutrientController> logger)
    {
        _nutrientCalculatorService = nutrientCalculatorService;
        _openAiService = openAiService;
        _ingredientService = ingredientService;
        _logger = logger;
    }

    [HttpPost("analyze-custom")]
    public async Task<ActionResult<ApiResponse<CustomMealNutritionResponseDto>>> AnalyzeCustomMealNutrition(
        [FromBody] CustomMealNutritionRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<CustomMealNutritionResponseDto>.ErrorResponse("Invalid request."));
            }

            if (string.IsNullOrWhiteSpace(request.MealDescription))
            {
                return BadRequest(ApiResponse<CustomMealNutritionResponseDto>.ErrorResponse("Meal description is required."));
            }

            if (request.Ingredients == null || !request.Ingredients.Any())
            {
                return BadRequest(ApiResponse<CustomMealNutritionResponseDto>.ErrorResponse("At least one ingredient is required."));
            }

            var invalidIngredient = request.Ingredients.FirstOrDefault(i =>
                string.IsNullOrWhiteSpace(i.IngredientName) ||
                string.IsNullOrWhiteSpace(i.Unit) ||
                i.Quantity <= 0);

            if (invalidIngredient != null)
            {
                return BadRequest(ApiResponse<CustomMealNutritionResponseDto>.ErrorResponse(
                    "Each ingredient must include name, quantity (> 0), and unit."));
            }

            var nutrientRequest = new NutrientPromptDto
            {
                Ingredients = request.Ingredients.Select(i => new IngredientPortionDto
                {
                    IngredientId = string.Empty,
                    IngredientName = i.IngredientName,
                    Quantity = i.Quantity,
                    Unit = i.Unit
                }).ToList()
            };

            var nutrientData = await _openAiService.CalculateNutrientsAsync(nutrientRequest);
            var conflicts = await BuildIngredientConflictsAsync(request.Ingredients);

            var adviceContext = $@"Meal: {request.MealDescription}
Calories: {nutrientData.TotalCalories}
Protein(g): {nutrientData.TotalProteins}
Carbs(g): {nutrientData.TotalCarbohydrates}
Fat(g): {nutrientData.TotalFats}
Conflicts: {(conflicts.Any() ? string.Join("; ", conflicts) : "No major ingredient conflicts detected")}";

            var healthAdvice = await _openAiService.GetHealthAdviceAsync(adviceContext);
            var cleanedAdvice = NormalizeAiAdvice(healthAdvice);
            var hasRealConflicts = conflicts.Any(c => !c.Equals("No major ingredient conflicts detected.", StringComparison.OrdinalIgnoreCase));

            var response = new CustomMealNutritionResponseDto
            {
                MealSummary = request.MealDescription,
                TotalCalories = nutrientData.TotalCalories,
                ProteinG = nutrientData.TotalProteins,
                CarbsG = nutrientData.TotalCarbohydrates,
                FatG = nutrientData.TotalFats,
                FiberG = GetDictionaryValue(nutrientData.Vitamins, "Fiber"),
                SugarG = GetDictionaryValue(nutrientData.Vitamins, "Sugar"),
                SodiumMg = GetDictionaryValue(nutrientData.Minerals, "Sodium"),
                IngredientConflicts = conflicts,
                BestConsumptionAdvice = cleanedAdvice,
                OverallNote = BuildOverallNote(
                    nutrientData.TotalCalories,
                    nutrientData.TotalProteins,
                    nutrientData.TotalCarbohydrates,
                    nutrientData.TotalFats,
                    GetDictionaryValue(nutrientData.Vitamins, "Fiber"),
                    GetDictionaryValue(nutrientData.Minerals, "Sodium"),
                    hasRealConflicts)
            };

            return Ok(ApiResponse<CustomMealNutritionResponseDto>.SuccessResponse(response));
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "AI service timeout during custom meal nutrition analysis");
            return StatusCode(408, ApiResponse<CustomMealNutritionResponseDto>.ErrorResponse(
                "AI service timeout. Please try again."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing custom meal nutrition");
            return StatusCode(500, ApiResponse<CustomMealNutritionResponseDto>.ErrorResponse(
                "An error occurred while analyzing custom meal nutrition."));
        }
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<NutrientCalculationDto>>> CalculateNutrients([FromBody] NutrientRequestDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<NutrientCalculationDto>.ErrorResponse("User not authenticated"));
            }

            var calculation = await _nutrientCalculatorService.CalculateNutrientsAsync(dto, userId);
            return Ok(ApiResponse<NutrientCalculationDto>.SuccessResponse(calculation, "Nutrients calculated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error calculating nutrients");
            return BadRequest(ApiResponse<NutrientCalculationDto>.ErrorResponse(ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "AI service timeout during nutrient calculation");
            return StatusCode(408, ApiResponse<NutrientCalculationDto>.ErrorResponse("AI service timeout. Please try again."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating nutrients");
            return StatusCode(500, ApiResponse<NutrientCalculationDto>.ErrorResponse("An error occurred while calculating nutrients"));
        }
    }

    [HttpPost("{calculationId}/save")]
    public async Task<ActionResult<ApiResponse<object>>> SaveCalculation(string calculationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _nutrientCalculatorService.SaveCalculationAsync(userId, calculationId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Calculation saved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Calculation not found: {CalculationId}", calculationId);
            return NotFound(ApiResponse<object>.ErrorResponse("Calculation not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving calculation");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while saving calculation"));
        }
    }

    [HttpGet("{calculationId}")]
    public async Task<ActionResult<ApiResponse<NutrientCalculationDto>>> GetSavedCalculation(string calculationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<NutrientCalculationDto>.ErrorResponse("User not authenticated"));
            }

            var calculation = await _nutrientCalculatorService.GetSavedCalculationAsync(calculationId, userId);
            if (calculation == null)
            {
                return NotFound(ApiResponse<NutrientCalculationDto>.ErrorResponse("Calculation not found"));
            }

            return Ok(ApiResponse<NutrientCalculationDto>.SuccessResponse(calculation, "Calculation retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calculation");
            return StatusCode(500, ApiResponse<NutrientCalculationDto>.ErrorResponse("An error occurred while retrieving calculation"));
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<NutrientCalculationDto>>>> GetUserCalculations()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<NutrientCalculationDto>>.ErrorResponse("User not authenticated"));
            }

            var calculations = await _nutrientCalculatorService.GetUserCalculationsAsync(userId);
            return Ok(ApiResponse<List<NutrientCalculationDto>>.SuccessResponse(calculations, "Calculation history retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calculation history");
            return StatusCode(500, ApiResponse<List<NutrientCalculationDto>>.ErrorResponse("An error occurred while retrieving calculation history"));
        }
    }

    private async Task<List<string>> BuildIngredientConflictsAsync(IEnumerable<CustomMealIngredientDto> ingredients)
    {
        var conflicts = new List<string>();

        foreach (var ingredient in ingredients)
        {
            var matches = await _ingredientService.SearchIngredientsWithoutHighlightAsync(ingredient.IngredientName);

            var matchedIngredient = matches.FirstOrDefault(i =>
                i.Name.Equals(ingredient.IngredientName, StringComparison.OrdinalIgnoreCase))
                ?? matches.FirstOrDefault();

            if (matchedIngredient?.Allergies != null && matchedIngredient.Allergies.Any())
            {
                var allergyNames = string.Join(", ", matchedIngredient.Allergies.Select(a => a.Name));
                conflicts.Add($"{ingredient.IngredientName} may trigger allergies: {allergyNames}.");
            }
        }

        if (!conflicts.Any())
        {
            conflicts.Add("No major ingredient conflicts detected.");
        }

        return conflicts;
    }

    private static decimal GetDictionaryValue(Dictionary<string, decimal>? values, string key)
    {
        if (values == null || values.Count == 0)
        {
            return 0;
        }

        var directMatch = values.FirstOrDefault(v => v.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(directMatch.Key))
        {
            return directMatch.Value;
        }

        var partialMatch = values.FirstOrDefault(v =>
            v.Key.Contains(key, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(partialMatch.Key) ? 0 : partialMatch.Value;
    }

    private static string BuildOverallNote(
        decimal calories,
        decimal proteinG,
        decimal carbsG,
        decimal fatG,
        decimal fiberG,
        decimal sodiumMg,
        bool hasRealConflicts)
    {
        var calorieNote = calories switch
        {
            < 350 => "This is a light meal.",
            >= 350 and <= 700 => "This is a moderate-calorie meal for most adults.",
            _ => "This is a higher-calorie meal; portion control may help depending on your goal."
        };

        var dominantMacro = new[]
        {
            (Name: "protein", Value: proteinG),
            (Name: "carbohydrate", Value: carbsG),
            (Name: "fat", Value: fatG)
        }
        .OrderByDescending(x => x.Value)
        .First().Name;

        var qualityTips = new List<string>();
        if (fiberG < 5)
        {
            qualityTips.Add("fiber is low, so add vegetables, legumes, or whole grains");
        }

        if (sodiumMg > 800)
        {
            qualityTips.Add("sodium is relatively high, so keep the next meal lower in salt");
        }

        var qualityNote = qualityTips.Count > 0
            ? $"Nutrient quality note: {string.Join("; ", qualityTips)}."
            : "Nutrient quality looks generally balanced for a single meal.";

        if (hasRealConflicts)
        {
            return $"{calorieNote} Dominant macro: {dominantMacro}. {qualityNote} Review ingredient conflict notes before regular consumption.";
        }

        return $"{calorieNote} Dominant macro: {dominantMacro}. {qualityNote}";
    }

    private static string NormalizeAiAdvice(string rawAdvice)
    {
        if (string.IsNullOrWhiteSpace(rawAdvice))
        {
            return "No advice provided.";
        }

        var text = rawAdvice.Trim();
        if (text.StartsWith("```") && text.EndsWith("```"))
        {
            text = text
                .TrimStart('`')
                .TrimEnd('`')
                .Trim();

            if (text.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                text = text[4..].Trim();
            }
        }

        if ((text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("[") && text.EndsWith("]")))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var preferredKeys = new[] { "health_advice", "advice", "message", "bestConsumptionAdvice", "note" };
                    foreach (var key in preferredKeys)
                    {
                        if (doc.RootElement.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                        {
                            var parsed = value.GetString();
                            if (!string.IsNullOrWhiteSpace(parsed))
                            {
                                return parsed.Trim();
                            }
                        }
                    }

                    foreach (var property in doc.RootElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.Value.GetString()))
                        {
                            return property.Value.GetString()!.Trim();
                        }
                    }
                }
            }
            catch
            {
                // Keep original cleaned string when parsing fails.
            }
        }

        return text;
    }
}
