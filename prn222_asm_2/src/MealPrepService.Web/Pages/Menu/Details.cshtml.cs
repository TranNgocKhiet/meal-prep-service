using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class DetailsModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IMenuService menuService, ILogger<DetailsModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public DailyMenuDto Menu { get; set; }

    // Helper properties for view binding
    public Guid Id => Menu?.Id ?? Guid.Empty;
    public DateTime MenuDate => Menu?.MenuDate ?? DateTime.MinValue;
    public string Status => Menu?.Status ?? string.Empty;
    public List<MenuMealDto> MenuMeals => Menu?.MenuMeals ?? new List<MenuMealDto>();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            // Find menu by ID (search through dates)
            DailyMenuDto? menuDto = null;
            
            // Search through recent dates to find the menu
            for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
            {
                var menu = await _menuService.GetByDateAsync(date);
                if (menu?.Id == id)
                {
                    menuDto = menu;
                    break;
                }
            }
            
            if (menuDto == null)
            {
                return NotFound("Menu not found.");
            }

            Menu = menuDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving menu {MenuId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the menu.";
            return RedirectToPage("/Menu/Index");
        }
    }
}
