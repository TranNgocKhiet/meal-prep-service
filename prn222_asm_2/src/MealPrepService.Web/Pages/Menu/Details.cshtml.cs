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
    public string ActiveTab { get; set; } = "current";
    public string Period { get; set; } = "month";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, string tab = "current", string period = "month", DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            ActiveTab = string.Equals(tab, "past", StringComparison.OrdinalIgnoreCase) ? "past" : "current";
            Period = period;
            FromDate = fromDate;
            ToDate = toDate;

            var (searchStartDate, searchEndDate) = ResolveSearchRange();

            // Find menu by ID (search through dates)
            DailyMenuDto? menuDto = null;
            
            for (var date = searchStartDate; date <= searchEndDate; date = date.AddDays(1))
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
            return RedirectToPage("/Menu/Index", new { tab = ActiveTab, period = Period, fromDate = FromDate, toDate = ToDate });
        }
    }

    private (DateTime SearchStartDate, DateTime SearchEndDate) ResolveSearchRange()
    {
        if (FromDate.HasValue && ToDate.HasValue)
        {
            var startDate = FromDate.Value.Date;
            var endDate = ToDate.Value.Date;

            if (endDate < startDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            return (startDate, endDate);
        }

        return (DateTime.Today.AddDays(-365), DateTime.Today.AddDays(365));
    }
}
