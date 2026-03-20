using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Diagnostics;
using System.Text.Json;

namespace MealPrepService.Web.Pages.AITest;

[Authorize]
public class MealNutritionAnalyzerModel : PageModel
{
    private readonly ILLMService _llmService;
    private readonly IAIOperationLogger _operationLogger;
    private readonly ILogger<MealNutritionAnalyzerModel> _logger;

    public MealNutritionAnalyzerModel(
        ILLMService llmService, 
        IAIOperationLogger operationLogger,
        ILogger<MealNutritionAnalyzerModel> logger)
    {
        _llmService = llmService;
        _operationLogger = operationLogger;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAnalyzeAsync([FromBody] CustomMealNutritionRequest? request)
    {
        if (request == null)
        {
            return new JsonResult(new { success = false, message = "Invalid request." });
        }

        if (string.IsNullOrWhiteSpace(request.MealDescription))
        {
            return new JsonResult(new { success = false, message = "Meal description is required." });
        }

        if (request.Ingredients == null || !request.Ingredients.Any())
        {
            return new JsonResult(new { success = false, message = "At least one ingredient is required." });
        }

        var invalidIngredient = request.Ingredients.FirstOrDefault(i =>
            string.IsNullOrWhiteSpace(i.IngredientName) ||
            string.IsNullOrWhiteSpace(i.Unit) ||
            i.Quantity <= 0);

        if (invalidIngredient != null)
        {
            return new JsonResult(new { success = false, message = "Each ingredient must include name, quantity (> 0), and unit." });
        }

        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? customerId = null;
        if (Guid.TryParse(accountIdClaim, out var parsedId))
        {
            customerId = parsedId;
        }

        var stopwatch = Stopwatch.StartNew();
        var inputParams = JsonSerializer.Serialize(new { 
            MealDescription = request.MealDescription, 
            IngredientCount = request.Ingredients.Count 
        });

        var operationLog = await _operationLogger.StartOperationAsync(
            "Nutrition Analysis", 
            inputParams, 
            customerId);

        try
        {
            var analysis = await _llmService.AnalyzeCustomMealNutritionAsync(request);
            
            stopwatch.Stop();
            var outputSummary = JsonSerializer.Serialize(new 
            { 
                success = true,
                totalCalories = analysis.TotalCalories
            });

            await _operationLogger.CompleteOperationAsync(
                operationLog.Id,
                "Success",
                outputSummary,
                (int)stopwatch.ElapsedMilliseconds);

            return new JsonResult(new { success = true, data = analysis });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await _operationLogger.FailOperationAsync(
                operationLog.Id,
                ex,
                (int)stopwatch.ElapsedMilliseconds);

            _logger.LogError(ex, "Failed to analyze custom meal nutrition");
            return new JsonResult(new { success = false, message = "Failed to analyze meal nutrition. Please try again." });
        }
    }
}
