using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MealPrepService.Web.Pages.AITest;

[Authorize]
public class MealNutritionAnalyzerModel : PageModel
{
    private readonly ILLMService _llmService;
    private readonly ILogger<MealNutritionAnalyzerModel> _logger;

    public MealNutritionAnalyzerModel(ILLMService llmService, ILogger<MealNutritionAnalyzerModel> logger)
    {
        _llmService = llmService;
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

        try
        {
            var analysis = await _llmService.AnalyzeCustomMealNutritionAsync(request);
            return new JsonResult(new { success = true, data = analysis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze custom meal nutrition");
            return new JsonResult(new { success = false, message = "Failed to analyze meal nutrition. Please try again." });
        }
    }
}
