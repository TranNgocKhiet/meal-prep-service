using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class EditModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IRecipeService recipeService, ILogger<EditModel> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    [BindProperty]
    public Guid Id { get; set; }
    
    [BindProperty]
    public string RecipeName { get; set; }
    
    [BindProperty]
    public string Instructions { get; set; }
    
    public float TotalCalories { get; set; }
    public float ProteinG { get; set; }
    public float FatG { get; set; }
    public float CarbsG { get; set; }
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var recipeDto = await _recipeService.GetByIdWithIngredientsAsync(id);
            
            if (recipeDto == null)
            {
                return NotFound("Recipe not found.");
            }

            Id = recipeDto.Id;
            RecipeName = recipeDto.RecipeName;
            Instructions = recipeDto.Instructions;
            TotalCalories = recipeDto.TotalCalories;
            ProteinG = recipeDto.ProteinG;
            FatG = recipeDto.FatG;
            CarbsG = recipeDto.CarbsG;
            Ingredients = recipeDto.Ingredients?.ToList() ?? new List<RecipeIngredientDto>();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading recipe {RecipeId} for editing", id);
            TempData["ErrorMessage"] = "An error occurred while loading the recipe.";
            return RedirectToPage("/Recipe/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Id == Guid.Empty)
        {
            return BadRequest("Recipe ID is required.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var updateDto = new UpdateRecipeDto
            {
                RecipeName = RecipeName,
                Instructions = Instructions
            };

            var updatedRecipe = await _recipeService.UpdateRecipeAsync(Id, updateDto);

            TempData["SuccessMessage"] = $"Recipe '{updatedRecipe.RecipeName}' updated successfully.";
            return RedirectToPage("/Recipe/Details", new { id = updatedRecipe.Id });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Recipe {RecipeId} not found", Id);
            return NotFound(ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating recipe {RecipeId}", Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while updating recipe {RecipeId}", Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating recipe {RecipeId}", Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the recipe.");
            return Page();
        }
    }
}
