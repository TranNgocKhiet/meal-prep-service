using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.Web.PresentationLayer.Cart;


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

    public async Task<IActionResult> OnPostAddToCartAsync(Guid menuMealId, int quantity = 1, DateTime? startDate = null)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Account/Login", new
            {
                returnUrl = Url.Page("/PublicMenu/Weekly", new { startDate = startDate?.ToString("yyyy-MM-dd") })
            });
        }

        if (!User.IsInRole("Customer"))
        {
            TempData["ErrorMessage"] = "Only customers can add items to cart.";
            return RedirectToPage(new { startDate });
        }

        try
        {
            var todayMenu = await _menuService.GetByDateAsync(DateTime.Today);
            var meal = todayMenu?.MenuMeals.FirstOrDefault(x => x.Id == menuMealId && !x.IsSoldOut);

            if (meal == null)
            {
                TempData["ErrorMessage"] = "Only meals from today's menu can be added to cart.";
                return RedirectToPage(new { startDate });
            }

            var cart = HttpContext.Session.GetCartItems();
            var existing = cart.FirstOrDefault(x => x.MenuMealId == menuMealId);

            if (existing == null)
            {
                cart.Add(new CartItemSession
                {
                    MenuMealId = menuMealId,
                    Quantity = Math.Max(1, quantity)
                });
            }
            else
            {
                existing.Quantity = Math.Min(existing.Quantity + Math.Max(1, quantity), meal.AvailableQuantity);
            }

            HttpContext.Session.SaveCartItems(cart);
            TempData["SuccessMessage"] = $"{meal.RecipeName} added to cart.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed adding menu meal {MenuMealId} to cart from weekly view", menuMealId);
            TempData["ErrorMessage"] = "Could not add this item to cart.";
        }

        return RedirectToPage(new { startDate });
    }
}
