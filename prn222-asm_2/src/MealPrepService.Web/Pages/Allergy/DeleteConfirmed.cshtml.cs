using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Allergy;

[Authorize(Roles = "Manager,Admin")]
public class DeleteConfirmedModel : PageModel
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<DeleteConfirmedModel> _logger;

    public DeleteConfirmedModel(IAllergyService allergyService, ILogger<DeleteConfirmedModel> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            await _allergyService.DeleteAsync(id);

            _logger.LogInformation("Allergy {AllergyId} deleted successfully", id);
            TempData["SuccessMessage"] = "Allergy deleted successfully!";
            return RedirectToPage("/Allergy/Index");
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Allergy/Index");
        }
        catch (ConstraintViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Allergy/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting allergy {AllergyId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the allergy. Please try again.";
            return RedirectToPage("/Allergy/Index");
        }
    }
}
