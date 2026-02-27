using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class AddModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly IIngredientService _ingredientService;
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<AddModel> _logger;

    [BindProperty]
    public Guid IngredientId { get; set; }

    [BindProperty]
    public float CurrentAmount { get; set; }

    [BindProperty]
    public DateTime ExpiryDate { get; set; } = DateTime.Today.AddDays(7);

    public List<IngredientDto> AvailableIngredients { get; set; } = new();

    public AddModel(
        IFridgeService fridgeService, 
        IIngredientService ingredientService, 
        IMealPlanService mealPlanService,
        ILogger<AddModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _ingredientService = ingredientService ?? throw new ArgumentNullException(nameof(ingredientService));
        _mealPlanService = mealPlanService ?? throw new ArgumentNullException(nameof(mealPlanService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            AvailableIngredients = (await _ingredientService.GetAllAsync()).ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading add fridge item form for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading the form.";
            return RedirectToPage("/Fridge/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            AvailableIngredients = (await _ingredientService.GetAllAsync()).ToList();
            return Page();
        }

        try
        {
            var accountId = GetCurrentAccountId();
            var ingredient = await _ingredientService.GetByIdAsync(IngredientId);
            
            if (ingredient == null)
            {
                ModelState.AddModelError(nameof(IngredientId), "Selected ingredient not found.");
                AvailableIngredients = (await _ingredientService.GetAllAsync()).ToList();
                return Page();
            }

            var fridgeItemDto = new FridgeItemDto
            {
                AccountId = accountId,
                IngredientId = IngredientId,
                IngredientName = ingredient.IngredientName,
                Unit = ingredient.Unit,
                CurrentAmount = CurrentAmount,
                ExpiryDate = ExpiryDate
            };

            await _fridgeService.AddItemAsync(fridgeItemDto);
            
            _logger.LogInformation("Fridge item {IngredientName} added successfully for account {AccountId}", 
                ingredient.IngredientName, accountId);
            
            TempData["SuccessMessage"] = $"{ingredient.IngredientName} added to your fridge successfully!";
            return RedirectToPage("/Fridge/Index");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableIngredients = (await _ingredientService.GetAllAsync()).ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding fridge item for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while adding the item. Please try again.");
            AvailableIngredients = (await _ingredientService.GetAllAsync()).ToList();
            return Page();
        }
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new AuthenticationException("User account ID not found in claims.");
        }
        return accountId;
    }
}
