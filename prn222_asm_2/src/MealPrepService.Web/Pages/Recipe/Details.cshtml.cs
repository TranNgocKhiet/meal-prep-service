using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class DetailsModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IRecipeService recipeService, ILogger<DetailsModel> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    public RecipeDto Recipe { get; set; }

    // Helper properties for view binding
    public Guid Id => Recipe?.Id ?? Guid.Empty;
    public string RecipeName => Recipe?.RecipeName ?? string.Empty;
    public string Instructions => Recipe?.Instructions ?? string.Empty;
    public float TotalCalories => Recipe?.TotalCalories ?? 0;
    public float ProteinG => Recipe?.ProteinG ?? 0;
    public float FatG => Recipe?.FatG ?? 0;
    public float CarbsG => Recipe?.CarbsG ?? 0;
    public List<RecipeIngredientDto> Ingredients => Recipe?.Ingredients ?? new List<RecipeIngredientDto>();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var recipeDto = await _recipeService.GetByIdWithIngredientsAsync(id);
            
            if (recipeDto == null)
            {
                return NotFound("Recipe not found.");
            }

            Recipe = recipeDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving recipe {RecipeId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the recipe.";
            return RedirectToPage("/Recipe/Index");
        }
    }
}
