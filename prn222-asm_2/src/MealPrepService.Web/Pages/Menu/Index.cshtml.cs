using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.Menu;

[Authorize(Roles = "Admin,Manager")]
public class IndexModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMenuService menuService, ILogger<IndexModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public List<DailyMenuDto> Menus { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ActiveTab { get; set; } = "current";

    public async Task<IActionResult> OnGetAsync(string tab = "current")
    {
        try
        {
            var menuList = new List<DailyMenuDto>();
            DateTime startDate;
            DateTime endDate;
            
            if (tab == "past")
            {
                // Get past menus (last 90 days)
                startDate = DateTime.Today.AddDays(-90);
                endDate = DateTime.Today.AddDays(-1);
            }
            else
            {
                // Get current and future menus (today + next 30 days)
                tab = "current";
                startDate = DateTime.Today;
                endDate = DateTime.Today.AddDays(30);
            }
            
            // Get menus for each day in the range
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var menu = await _menuService.GetByDateAsync(date);
                if (menu != null)
                {
                    menuList.Add(menu);
                }
            }

            Menus = menuList.OrderByDescending(m => m.MenuDate).ToList();
            StartDate = startDate;
            EndDate = endDate;
            ActiveTab = tab;
            
            // Set ViewData properties for the view
            ViewData["StartDate"] = startDate;
            ViewData["EndDate"] = endDate;
            ViewData["ActiveTab"] = tab;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving menus");
            TempData["ErrorMessage"] = "An error occurred while loading the menus.";
            Menus = new List<DailyMenuDto>();
            return Page();
        }
    }
}
