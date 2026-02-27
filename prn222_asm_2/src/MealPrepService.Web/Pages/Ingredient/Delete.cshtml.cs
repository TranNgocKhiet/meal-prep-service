using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Ingredient;

[Authorize(Roles = "Admin,Manager")]
public class DeleteModel : PageModel
{
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(IIngredientService ingredientService, ILogger<DeleteModel> logger)
    {
        _ingredientService = ingredientService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            await _ingredientService.DeleteIngredientAsync(id);

            TempData["SuccessMessage"] = "Ingredient deleted successfully.";
            return RedirectToPage("/Ingredient/Index");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Ingredient {IngredientId} not found", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Ingredient/Index");
        }
        catch (ConstraintViolationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete ingredient {IngredientId} due to constraint violation", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Ingredient/Details", new { id });
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while deleting ingredient {IngredientId}", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Ingredient/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting ingredient {IngredientId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the ingredient.";
            return RedirectToPage("/Ingredient/Details", new { id });
        }
    }
}
