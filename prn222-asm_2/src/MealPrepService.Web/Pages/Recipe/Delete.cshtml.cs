using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Recipe;

[Authorize(Roles = "Admin,Manager")]
public class DeleteModel : PageModel
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(IRecipeService recipeService, ILogger<DeleteModel> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            await _recipeService.DeleteRecipeAsync(id);

            TempData["SuccessMessage"] = "Recipe deleted successfully.";
            return RedirectToPage("/Recipe/Index");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Recipe {RecipeId} not found", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Recipe/Index");
        }
        catch (ConstraintViolationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete recipe {RecipeId} due to constraint violation", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Recipe/Details", new { id });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while deleting recipe {RecipeId}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Recipe/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting recipe {RecipeId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the recipe.";
            return RedirectToPage("/Recipe/Details", new { id });
        }
    }
}
