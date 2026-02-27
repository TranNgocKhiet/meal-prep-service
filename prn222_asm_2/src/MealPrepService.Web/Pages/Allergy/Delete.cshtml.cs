using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;


namespace MealPrepService.Web.Pages.Allergy;

[Authorize(Roles = "Manager,Admin")]
public class DeleteModel : PageModel
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(IAllergyService allergyService, ILogger<DeleteModel> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    [BindProperty]
    public Guid Id { get; set; }
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
            _logger.LogError(ex, "Error loading allergy {AllergyId} for deletion", id);
            TempData["ErrorMessage"] = "An error occurred while loading the allergy.";
            return RedirectToPage("/Allergy/Index");
        }
    }
}
