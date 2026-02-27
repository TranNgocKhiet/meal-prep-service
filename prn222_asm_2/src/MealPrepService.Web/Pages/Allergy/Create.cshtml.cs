using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Allergy;

[Authorize(Roles = "Manager,Admin")]
public class CreateModel : PageModel
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IAllergyService allergyService, ILogger<CreateModel> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    [BindProperty]
    public string AllergyName { get; set; } = string.Empty;

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
            var createDto = new CreateAllergyDto
            {
                AllergyName = AllergyName
            };

            await _allergyService.CreateAsync(createDto);

            _logger.LogInformation("Allergy {AllergyName} created successfully", AllergyName);
            TempData["SuccessMessage"] = $"Allergy '{AllergyName}' created successfully!";
            return RedirectToPage("/Allergy/Index");
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating allergy");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the allergy. Please try again.");
            return Page();
        }
    }
}
