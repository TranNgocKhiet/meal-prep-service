using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class AddMealModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly IRecipeService _recipeService;
    private readonly ILogger<AddMealModel> _logger;

    public AddMealModel(IMealPlanService mealPlanService, IRecipeService recipeService, ILogger<AddMealModel> logger)
    {
        _mealPlanService = mealPlanService;
        _recipeService = recipeService;
        _logger = logger;
    }

    [BindProperty]
    public Guid PlanId { get; set; }

    [BindProperty]
    public string MealType { get; set; } = string.Empty;

    [BindProperty]
    public DateTime ServeDate { get; set; }

    [BindProperty]
    public List<Guid> SelectedRecipeIds { get; set; } = new();

    public List<RecipeDto> AvailableRecipes { get; set; } = new();
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public bool ShowAll { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    
    // Helper properties for view
    public bool HasRecipes => AvailableRecipes?.Any() ?? false;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    
    // Static helper for meal type options
    public static List<string> MealTypeOptions => new() { "Breakfast", "Lunch", "Dinner", "Snack" };

    public async Task<IActionResult> OnGetAsync(Guid planId, string searchTerm = "", bool showAll = false, int page = 1)
    {
        try
        {
            const int pageSize = 30;
            
            var mealPlan = await _mealPlanService.GetByIdAsync(planId);
            
            if (mealPlan == null)
            {
                return NotFound("Meal plan not found.");
            }

            var accountId = GetCurrentAccountId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (mealPlan.AccountId != accountId && userRole != "Manager")
            {
                return Forbid("You don't have permission to modify this meal plan.");
            }

            var recipes = new List<RecipeDto>();
            if (!string.IsNullOrWhiteSpace(searchTerm) || showAll)
            {
                var allRecipes = await _recipeService.GetAllWithIngredientsAsync();
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    recipes = allRecipes
                        .Where(r => r.RecipeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else if (showAll)
                {
                    recipes = allRecipes.ToList();
                }
            }

            var totalItems = recipes.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
            
            AvailableRecipes = recipes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            PlanId = planId;
            ServeDate = mealPlan.StartDate;
            SearchTerm = searchTerm;
            ShowAll = showAll;
            CurrentPage = page;
            TotalPages = totalPages;
            PageSize = pageSize;
            TotalItems = totalItems;
            PlanName = mealPlan.PlanName;
            StartDate = mealPlan.StartDate;
            EndDate = mealPlan.EndDate;
            
            // Set ViewData properties for the view
            ViewData["PlanName"] = PlanName;
            ViewData["StartDate"] = StartDate;
            ViewData["EndDate"] = EndDate;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading add meal form for plan {PlanId}", planId);
            TempData["ErrorMessage"] = "An error occurred while loading the form.";
            return RedirectToPage("/MealPlan/Details", new { id = planId });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            AvailableRecipes = (await _recipeService.GetAllWithIngredientsAsync()).ToList();
            return Page();
        }

        try
        {
            var selectedRecipes = new List<RecipeDto>();
            foreach (var recipeId in SelectedRecipeIds)
            {
                var recipe = await _recipeService.GetByIdAsync(recipeId);
                if (recipe != null)
                {
                    selectedRecipes.Add(recipe);
                }
            }

            var mealDto = new MealDto
            {
                PlanId = PlanId,
                MealType = MealType,
                ServeDate = ServeDate,
                Recipes = selectedRecipes
            };

            await _mealPlanService.AddMealToPlanAsync(PlanId, mealDto);
            
            _logger.LogInformation("Meal added successfully to plan {PlanId}", PlanId);
            
            TempData["SuccessMessage"] = "Meal added to plan successfully!";
            return RedirectToPage("/MealPlan/Details", new { id = PlanId });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableRecipes = (await _recipeService.GetAllWithIngredientsAsync()).ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding meal to plan {PlanId}", PlanId);
            ModelState.AddModelError(string.Empty, "An error occurred while adding the meal. Please try again.");
            AvailableRecipes = (await _recipeService.GetAllWithIngredientsAsync()).ToList();
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
