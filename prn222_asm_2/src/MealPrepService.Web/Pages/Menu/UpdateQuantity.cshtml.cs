using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class UpdateQuantityModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<UpdateQuantityModel> _logger;

    public UpdateQuantityModel(IMenuService menuService, ILogger<UpdateQuantityModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    [BindProperty]
    public Guid MenuMealId { get; set; }
    
    [BindProperty]
    public int NewQuantity { get; set; }
    
    public string RecipeName { get; set; }
    public int CurrentQuantity { get; set; }
    public Guid MenuId { get; set; }
    public string MenuDate { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid menuMealId)
    {
        try
        {
            // Find the menu meal by searching through menus
            MenuMealDto? menuMealDto = null;
            DailyMenuDto? parentMenu = null;
            
            // Search through recent dates to find the menu meal
            for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
            {
                var menu = await _menuService.GetByDateAsync(date);
                if (menu != null)
                {
                    var meal = menu.MenuMeals.FirstOrDefault(m => m.Id == menuMealId);
                    if (meal != null)
                    {
                        menuMealDto = meal;
                        parentMenu = menu;
                        break;
                    }
                }
            }
            
            if (menuMealDto == null || parentMenu == null)
            {
                return NotFound("Menu meal not found.");
            }

            // Check if menu is in draft or inactive status (can be edited)
            if (parentMenu.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Cannot update quantities for an active menu. Please deactivate it first.";
                return RedirectToPage("/Menu/Details", new { id = parentMenu.Id });
            }

            MenuMealId = menuMealId;
            NewQuantity = menuMealDto.AvailableQuantity;
            RecipeName = menuMealDto.RecipeName;
            CurrentQuantity = menuMealDto.AvailableQuantity;
            MenuId = parentMenu.Id;
            MenuDate = parentMenu.MenuDate.ToString("dddd, MMMM dd, yyyy");
            
            // Set ViewData properties for the view
            ViewData["MenuDate"] = MenuDate;
            ViewData["MenuId"] = MenuId;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading update quantity form for menu meal {MenuMealId}", menuMealId);
            TempData["ErrorMessage"] = "An error occurred while loading the form.";
            return RedirectToPage("/Menu/Index");
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
            await _menuService.UpdateMealQuantityAsync(MenuMealId, NewQuantity);
            
            _logger.LogInformation("Menu meal {MenuMealId} quantity updated to {NewQuantity}", 
                MenuMealId, NewQuantity);
            
            TempData["SuccessMessage"] = "Quantity updated successfully!";
            
            // Find the parent menu to redirect back to details
            for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
            {
                var menu = await _menuService.GetByDateAsync(date);
                if (menu != null && menu.MenuMeals.Any(m => m.Id == MenuMealId))
                {
                    return RedirectToPage("/Menu/Details", new { id = menu.Id });
                }
            }
            
            return RedirectToPage("/Menu/Index");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating quantity for menu meal {MenuMealId}", MenuMealId);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the quantity. Please try again.");
            return Page();
        }
    }
}
