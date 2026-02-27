using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class GroceryListModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly IIngredientService _ingredientService;
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<GroceryListModel> _logger;

    [BindProperty]
    public Guid MealPlanId { get; set; }

    public List<MealPlanDto> AvailableMealPlans { get; set; } = new();
    public GroceryListDto? ResultGroceryList { get; set; }
    public bool ShowResult { get; set; }

    // Helper properties for view binding
    public string MealPlanName => ResultGroceryList?.MealPlanName ?? string.Empty;
    public DateTime GeneratedDate => ResultGroceryList?.GeneratedDate ?? DateTime.Now;
    public List<GroceryItemDto> MissingIngredients => ResultGroceryList?.MissingIngredients ?? new List<GroceryItemDto>();
    public int TotalMissingItems => MissingIngredients.Count;

    public GroceryListModel(
        IFridgeService fridgeService, 
        IIngredientService ingredientService, 
        IMealPlanService mealPlanService,
        ILogger<GroceryListModel> logger)
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
            var accountId = GetCurrentAccountId();
            
            // Try to get active meal plan
            var activePlan = await _mealPlanService.GetActivePlanAsync(accountId);
            
            if (activePlan != null)
            {
                // Auto-generate grocery list from active plan
                try
                {
                    var groceryListDto = await _fridgeService.GenerateGroceryListFromActivePlanAsync(accountId);
                    
                    ResultGroceryList = groceryListDto;
                    ShowResult = true;
                    _logger.LogInformation("Grocery list auto-generated from active plan for account {AccountId}", accountId);
                    
                    return Page();
                }
                catch (BusinessException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }
            }
            
            // No active plan or error - show meal plan selection form
            var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
            
            AvailableMealPlans = mealPlans.ToList();
            
            if (!mealPlans.Any())
            {
                TempData["InfoMessage"] = "You don't have any meal plans yet. Create a meal plan first to generate a grocery list.";
            }
            else if (activePlan == null)
            {
                TempData["InfoMessage"] = "No active meal plan set. Please select a meal plan below or set one as active from your meal plans.";
            }
            
            ShowResult = false;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading grocery list for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading the grocery list.";
            return RedirectToPage("/Fridge/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Reload meal plans for the form
            var accountId = GetCurrentAccountId();
            var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
            AvailableMealPlans = mealPlans.ToList();
            
            ShowResult = false;
            return Page();
        }

        try
        {
            var accountId = GetCurrentAccountId();
            var groceryListDto = await _fridgeService.GenerateGroceryListAsync(accountId, MealPlanId);
            
            ResultGroceryList = groceryListDto;
            ShowResult = true;
            _logger.LogInformation("Grocery list generated successfully for account {AccountId} and meal plan {MealPlanId}", 
                accountId, MealPlanId);
            
            return Page();
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            
            // Reload meal plans for the form
            var accountId = GetCurrentAccountId();
            var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
            AvailableMealPlans = mealPlans.ToList();
            
            ShowResult = false;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating grocery list for account {AccountId} and meal plan {MealPlanId}", 
                GetCurrentAccountId(), MealPlanId);
            ModelState.AddModelError(string.Empty, "An error occurred while generating the grocery list. Please try again.");
            
            // Reload meal plans for the form
            var accountId = GetCurrentAccountId();
            var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
            AvailableMealPlans = mealPlans.ToList();
            
            ShowResult = false;
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
