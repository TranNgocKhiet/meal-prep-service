using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class AddIngredientModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<AddIngredientModel> _logger;

    public AddIngredientModel(IRecipeService recipeService, IIngredientService ingredientService, ILogger<AddIngredientModel> logger)
    {
        _recipeService = recipeService;
        _ingredientService = ingredientService;
        _logger = logger;
    }

    [BindProperty]
    public Guid RecipeId { get; set; }
    
    [BindProperty]
    public Guid IngredientId { get; set; }
    
    [BindProperty]
    public float Amount { get; set; }
    
    public string RecipeName { get; set; }
    public List<IngredientDto> AvailableIngredients { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var recipeDto = await _recipeService.GetByIdAsync(id);
            
            if (recipeDto == null)
            {
                return NotFound("Recipe not found.");
            }

            // Get all available ingredients
            var ingredientDtos = await _ingredientService.GetAllAsync();
            
            RecipeId = id;
            RecipeName = recipeDto.RecipeName;
            AvailableIngredients = ingredientDtos.ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading add ingredient form for recipe {RecipeId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the form.";
            return RedirectToPage("/Recipe/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Reload available ingredients
            var ingredientDtos = await _ingredientService.GetAllAsync();
            AvailableIngredients = ingredientDtos.ToList();
            return Page();
        }

        try
        {
            var ingredientDto = new RecipeIngredientDto
            {
                IngredientId = IngredientId,
                Amount = Amount
            };

            await _recipeService.AddIngredientToRecipeAsync(RecipeId, ingredientDto);

            TempData["SuccessMessage"] = "Ingredient added to recipe successfully.";
            return RedirectToPage("/Recipe/Details", new { id = RecipeId });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Recipe or ingredient not found");
            ModelState.AddModelError(string.Empty, ex.Message);
            
            // Reload available ingredients
            var ingredientDtos = await _ingredientService.GetAllAsync();
            AvailableIngredients = ingredientDtos.ToList();
            return Page();
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while adding ingredient to recipe");
            ModelState.AddModelError(string.Empty, ex.Message);
            
            // Reload available ingredients
            var ingredientDtos = await _ingredientService.GetAllAsync();
            AvailableIngredients = ingredientDtos.ToList();
            return Page();
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while adding ingredient to recipe");
            ModelState.AddModelError(string.Empty, ex.Message);
            
            // Reload available ingredients
            var ingredientDtos = await _ingredientService.GetAllAsync();
            AvailableIngredients = ingredientDtos.ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding ingredient to recipe {RecipeId}", RecipeId);
            ModelState.AddModelError(string.Empty, "An error occurred while adding the ingredient.");
            
            // Reload available ingredients
            var ingredientDtos = await _ingredientService.GetAllAsync();
            AvailableIngredients = ingredientDtos.ToList();
            return Page();
        }
    }
}
