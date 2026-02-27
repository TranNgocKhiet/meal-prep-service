using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class RemoveIngredientModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<RemoveIngredientModel> _logger;

    public RemoveIngredientModel(IRecipeService recipeService, ILogger<RemoveIngredientModel> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid recipeId, Guid ingredientId)
    {
        try
        {
            await _recipeService.RemoveIngredientFromRecipeAsync(recipeId, ingredientId);

            TempData["SuccessMessage"] = "Ingredient removed from recipe successfully.";
            return RedirectToPage("/Recipe/Edit", new { id = recipeId });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while removing ingredient from recipe");
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Recipe/Edit", new { id = recipeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing ingredient from recipe {RecipeId}", recipeId);
            TempData["ErrorMessage"] = "An error occurred while removing the ingredient.";
            return RedirectToPage("/Recipe/Edit", new { id = recipeId });
        }
    }
}
