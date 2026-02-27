using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class CreateModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IRecipeService recipeService, ILogger<CreateModel> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    [BindProperty]
    public string RecipeName { get; set; }
    
    [BindProperty]
    public string Instructions { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var createDto = new CreateRecipeDto
            {
                RecipeName = RecipeName,
                Instructions = Instructions
            };

            var createdRecipe = await _recipeService.CreateRecipeAsync(createDto);

            TempData["SuccessMessage"] = $"Recipe '{createdRecipe.RecipeName}' created successfully.";
            return RedirectToPage("/Recipe/Details", new { id = createdRecipe.Id });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating recipe");
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while creating recipe");
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating recipe");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the recipe.");
            return Page();
        }
    }
}
