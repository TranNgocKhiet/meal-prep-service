using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Ingredient;

[Authorize(Roles = "Admin,Manager")]
public class CreateModel : PageModel
{
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IIngredientService ingredientService, ILogger<CreateModel> logger)
    {
        _ingredientService = ingredientService;
        _logger = logger;
    }

    [BindProperty]
    public string IngredientName { get; set; } = string.Empty;

    [BindProperty]
    public string Unit { get; set; } = string.Empty;

    [BindProperty]
    public float CaloPerUnit { get; set; }

    [BindProperty]
    public bool IsAllergen { get; set; }

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
            var createDto = new CreateIngredientDto
            {
                IngredientName = IngredientName,
                Unit = Unit,
                CaloPerUnit = CaloPerUnit,
                IsAllergen = IsAllergen
            };

            var createdIngredient = await _ingredientService.CreateIngredientAsync(createDto);

            TempData["SuccessMessage"] = $"Ingredient '{createdIngredient.IngredientName}' created successfully.";
            return RedirectToPage("/Ingredient/Index");
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating ingredient");
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while creating ingredient");
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating ingredient");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the ingredient.");
            return Page();
        }
    }
}
