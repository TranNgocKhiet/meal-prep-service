using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Allergy;

[Authorize(Roles = "Manager,Admin")]
public class EditModel : PageModel
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IAllergyService allergyService, ILogger<EditModel> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string AllergyName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var allergy = await _allergyService.GetByIdAsync(id);
            if (allergy == null)
            {
                TempData["ErrorMessage"] = "Allergy not found.";
                return RedirectToPage("/Allergy/Index");
            }

            Id = allergy.Id;
            AllergyName = allergy.AllergyName;

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading allergy {AllergyId} for editing", id);
            TempData["ErrorMessage"] = "An error occurred while loading the allergy.";
            return RedirectToPage("/Allergy/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var updateDto = new UpdateAllergyDto
            {
                Id = Id,
                AllergyName = AllergyName
            };

            await _allergyService.UpdateAsync(updateDto);

            _logger.LogInformation("Allergy {AllergyId} updated successfully", Id);
            TempData["SuccessMessage"] = $"Allergy '{AllergyName}' updated successfully!";
            return RedirectToPage("/Allergy/Index");
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/Allergy/Index");
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergy {AllergyId}", Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the allergy. Please try again.");
            return Page();
        }
    }
}
