using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class UpdateIngredientModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<UpdateIngredientModel> _logger;

    public UpdateIngredientModel(IRecipeService recipeService, ILogger<UpdateIngredientModel> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid recipeId, Guid ingredientId, float amount)
    {
        try
        {
            await _recipeService.UpdateRecipeIngredientAsync(recipeId, ingredientId, amount);

            TempData["SuccessMessage"] = "Ingredient amount updated successfully.";
            return RedirectToPage("/Recipe/Edit", new { id = recipeId });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while updating ingredient in recipe");
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Recipe/Edit", new { id = recipeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating ingredient in recipe {RecipeId}", recipeId);
            TempData["ErrorMessage"] = "An error occurred while updating the ingredient.";
            return RedirectToPage("/Recipe/Edit", new { id = recipeId });
        }
    }
}
