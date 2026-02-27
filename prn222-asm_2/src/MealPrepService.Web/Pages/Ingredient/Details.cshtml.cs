using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Ingredient;

[Authorize(Roles = "Admin,Manager")]
public class DetailsModel : PageModel
{
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IIngredientService ingredientService, ILogger<DetailsModel> logger)
    {
        _ingredientService = ingredientService;
        _logger = logger;
    }

    public IngredientDto Ingredient { get; set; } = new();

    // Helper properties for view binding
    public Guid Id => Ingredient.Id;
    public string IngredientName => Ingredient.IngredientName;
    public string Unit => Ingredient.Unit;
    public float CaloPerUnit => Ingredient.CaloPerUnit;
    public bool IsAllergen => Ingredient.IsAllergen;
    public List<AllergyDto> Allergies => Ingredient.Allergies;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var ingredientDto = await _ingredientService.GetByIdAsync(id);
            
            if (ingredientDto == null)
            {
                return NotFound("Ingredient not found.");
            }

            Ingredient = ingredientDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving ingredient {IngredientId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the ingredient.";
            return RedirectToPage("/Ingredient/Index");
        }
    }
}
