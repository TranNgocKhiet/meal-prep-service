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
    public string Period { get; set; } = "month";

    public async Task<IActionResult> OnGetAsync(string tab = "current", string period = "month", DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var menuList = new List<DailyMenuDto>();
            tab = string.Equals(tab, "past", StringComparison.OrdinalIgnoreCase) ? "past" : "current";
            period = NormalizePeriod(period);

            var (startDate, endDate) = ResolveDateRange(tab, period, fromDate, toDate);
            
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
            Period = period;
            
            // Set ViewData properties for the view
            ViewData["StartDate"] = startDate;
            ViewData["EndDate"] = endDate;
            ViewData["ActiveTab"] = tab;
            ViewData["Period"] = period;
            ViewData["FromDate"] = startDate;
            ViewData["ToDate"] = endDate;
            
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

    private static string NormalizePeriod(string? period)
    {
        return period?.ToLowerInvariant() switch
        {
            "week" => "week",
            "month" => "month",
            "quarter" => "quarter",
            "year" => "year",
            "custom" => "custom",
            _ => "month"
        };
    }

    private static (DateTime StartDate, DateTime EndDate) ResolveDateRange(string tab, string period, DateTime? fromDate, DateTime? toDate)
    {
        if (period == "custom" && fromDate.HasValue && toDate.HasValue)
        {
            var customStart = fromDate.Value.Date;
            var customEnd = toDate.Value.Date;
            if (customEnd < customStart)
            {
                (customStart, customEnd) = (customEnd, customStart);
            }

            return (customStart, customEnd);
        }

        var today = DateTime.Today;
        var days = period switch
        {
            "week" => 7,
            "quarter" => 90,
            "year" => 365,
            _ => 30
        };

        return tab == "past"
            ? (today.AddDays(-days), today.AddDays(-1))
            : (today, today.AddDays(days - 1));
    }
}
