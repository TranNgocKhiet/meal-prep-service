using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Ingredient;

[Authorize(Roles = "Admin,Manager")]
public class EditModel : PageModel
{
    private readonly IIngredientService _ingredientService;
    private readonly IAllergyService _allergyService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IIngredientService ingredientService, IAllergyService allergyService, ILogger<EditModel> logger)
    {
        _ingredientService = ingredientService;
        _allergyService = allergyService;
        _logger = logger;
    }

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string IngredientName { get; set; } = string.Empty;

    [BindProperty]
    public string Unit { get; set; } = string.Empty;

    [BindProperty]
    public float CaloPerUnit { get; set; }

    [BindProperty]
    public bool IsAllergen { get; set; }

    [BindProperty]
    public List<Guid> SelectedAllergyIds { get; set; } = new();

    public List<AllergyDto> AvailableAllergies { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var ingredientDto = await _ingredientService.GetByIdAsync(id);
            
            if (ingredientDto == null)
            {
                return NotFound("Ingredient not found.");
            }

            var allAllergies = await _allergyService.GetAllAsync();

            Id = ingredientDto.Id;
            IngredientName = ingredientDto.IngredientName;
            Unit = ingredientDto.Unit;
            CaloPerUnit = ingredientDto.CaloPerUnit;
            IsAllergen = ingredientDto.IsAllergen;
            SelectedAllergyIds = ingredientDto.Allergies.Select(a => a.Id).ToList();
            AvailableAllergies = allAllergies.ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading ingredient {IngredientId} for editing", id);
            TempData["ErrorMessage"] = "An error occurred while loading the ingredient.";
            return RedirectToPage("/Ingredient/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Id == Guid.Empty)
        {
            return BadRequest("Ingredient ID is required.");
        }

        if (!ModelState.IsValid)
        {
            AvailableAllergies = (await _allergyService.GetAllAsync()).ToList();
            return Page();
        }

        try
        {
            var updateDto = new UpdateIngredientDto
            {
                IngredientName = IngredientName,
                Unit = Unit,
                CaloPerUnit = CaloPerUnit,
                IsAllergen = IsAllergen,
                AllergyIds = SelectedAllergyIds ?? new List<Guid>()
            };

            var updatedIngredient = await _ingredientService.UpdateIngredientAsync(Id, updateDto);

            TempData["SuccessMessage"] = $"Ingredient '{updatedIngredient.IngredientName}' updated successfully.";
            return RedirectToPage("/Ingredient/Index");
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Ingredient {IngredientId} not found", Id);
            return NotFound(ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating ingredient {IngredientId}", Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableAllergies = (await _allergyService.GetAllAsync()).ToList();
            return Page();
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(ex, "Business error while updating ingredient {IngredientId}", Id);
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableAllergies = (await _allergyService.GetAllAsync()).ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating ingredient {IngredientId}", Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the ingredient.");
            AvailableAllergies = (await _allergyService.GetAllAsync()).ToList();
            return Page();
        }
    }
}
