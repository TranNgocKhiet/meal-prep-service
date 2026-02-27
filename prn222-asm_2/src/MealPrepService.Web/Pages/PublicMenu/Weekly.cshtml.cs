using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.PublicMenu;

[AllowAnonymous]
public class WeeklyModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<WeeklyModel> _logger;

    public WeeklyModel(IMenuService menuService, ILogger<WeeklyModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public List<DailyMenuDto> DailyMenus { get; set; } = new();
    public string ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(DateTime? startDate = null)
    {
        try
        {
            var weekStart = startDate ?? GetStartOfWeek(DateTime.Today);
            var weekEnd = weekStart.AddDays(6);
            
            var weeklyMenus = await _menuService.GetWeeklyMenuAsync(weekStart);
            
            var dailyMenuList = new List<DailyMenuDto>();
            
            for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
            {
                var menuForDate = weeklyMenus.FirstOrDefault(m => m.MenuDate.Date == date.Date);
                
                if (menuForDate != null && menuForDate.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    dailyMenuList.Add(menuForDate);
                }
                else
                {
                    // Add empty menu for the date
                    dailyMenuList.Add(new DailyMenuDto
                    {
                        MenuDate = date,
                        Status = "inactive",
                        MenuMeals = new List<MenuMealDto>()
                    });
                }
            }

            WeekStartDate = weekStart;
            WeekEndDate = weekEnd;
            DailyMenus = dailyMenuList;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving weekly menu for week starting {StartDate}", startDate);
            
            var weekStart = startDate ?? GetStartOfWeek(DateTime.Today);
            WeekStartDate = weekStart;
            WeekEndDate = weekStart.AddDays(6);
            DailyMenus = new List<DailyMenuDto>();
            
            ErrorMessage = "An error occurred while loading the weekly menu. Please try again later.";
            return Page();
        }
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
