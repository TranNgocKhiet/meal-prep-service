using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.Hubs;


namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class AddMealModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly IRecipeService _recipeService;
    private readonly IHubContext<MenuHub> _menuHubContext;
    private readonly ILogger<AddMealModel> _logger;

    public AddMealModel(IMenuService menuService, IRecipeService recipeService, IHubContext<MenuHub> menuHubContext, ILogger<AddMealModel> logger)
    {
        _menuService = menuService;
        _recipeService = recipeService;
        _menuHubContext = menuHubContext;
        _logger = logger;
    }

    [BindProperty]
    public Guid MenuId { get; set; }
    
    [BindProperty]
    public Guid RecipeId { get; set; }
    
    [BindProperty]
    public decimal Price { get; set; }
    
    [BindProperty]
    public int AvailableQuantity { get; set; }

    public List<RecipeDto> AvailableRecipes { get; set; } = new();
    public string MenuDateDisplay { get; set; }
    
    private DateTime? _menuDate;

    public async Task<IActionResult> OnGetAsync(Guid menuId)
    {
        try
        {
            // Find menu by ID
            DailyMenuDto? menuDto = null;
            
            // Search through recent dates to find the menu
            for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
            {
                var menu = await _menuService.GetByDateAsync(date);
                if (menu?.Id == menuId)
                {
                    menuDto = menu;
                    break;
                }
            }
            
            if (menuDto == null)
            {
                return NotFound("Menu not found.");
            }

            // Check if menu is in draft or inactive status (can be edited)
            if (menuDto.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Cannot add meals to an active menu. Please deactivate it first.";
                return RedirectToPage("/Menu/Details", new { id = menuId });
            }

            var recipes = await _recipeService.GetAllAsync();
            
            MenuId = menuId;
            AvailableRecipes = recipes.ToList();
            MenuDateDisplay = menuDto.MenuDate.ToString("dddd, MMMM dd, yyyy");
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading add meal form for menu {MenuId}", menuId);
            TempData["ErrorMessage"] = "An error occurred while loading the form.";
            return RedirectToPage("/Menu/Details", new { id = menuId });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Reload recipes for the form
            var recipes = await _recipeService.GetAllAsync();
            AvailableRecipes = recipes.ToList();
            return Page();
        }

        try
        {
            var recipe = await _recipeService.GetByIdAsync(RecipeId);
            if (recipe == null)
            {
                ModelState.AddModelError(nameof(RecipeId), "Selected recipe not found.");
                
                // Reload recipes for the form
                var recipes = await _recipeService.GetAllAsync();
                AvailableRecipes = recipes.ToList();
                return Page();
            }

            var menuMealDto = new MenuMealDto
            {
                MenuId = MenuId,
                RecipeId = RecipeId,
                RecipeName = recipe.RecipeName,
                Price = Price,
                AvailableQuantity = AvailableQuantity,
                IsSoldOut = false,
                Recipe = recipe
            };

            await _menuService.AddMealToMenuAsync(MenuId, menuMealDto);
            
            _logger.LogInformation("Meal {RecipeName} added successfully to menu {MenuId}", recipe.RecipeName, MenuId);
            
            // Send SignalR notification about menu update
            await _menuHubContext.Clients.All.SendAsync("ReceiveMenuUpdate", 
                DateTime.UtcNow, 
                RecipeId.ToString(), 
                AvailableQuantity);
            
            TempData["SuccessMessage"] = "Meal added to menu successfully!";
            return RedirectToPage("/Menu/Details", new { id = MenuId });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            
            // Reload recipes for the form
            var recipes = await _recipeService.GetAllAsync();
            AvailableRecipes = recipes.ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding meal to menu {MenuId}", MenuId);
            ModelState.AddModelError(string.Empty, "An error occurred while adding the meal. Please try again.");
            
            // Reload recipes for the form
            var recipes = await _recipeService.GetAllAsync();
            AvailableRecipes = recipes.ToList();
            return Page();
        }
    }
}
