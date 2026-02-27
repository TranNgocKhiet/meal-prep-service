using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.PublicMenu;

[AllowAnonymous]
public class DateModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<DateModel> _logger;

    public DateModel(IMenuService menuService, ILogger<DateModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public DailyMenuDto Menu { get; set; }
    public string NoMenuMessage { get; set; }
    public string ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(DateTime date)
    {
        try
        {
            var menuDto = await _menuService.GetByDateAsync(date.Date);
            
            if (menuDto == null || !menuDto.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                Menu = new DailyMenuDto
                {
                    MenuDate = date.Date,
                    Status = "inactive",
                    MenuMeals = new List<MenuMealDto>()
                };
                
                NoMenuMessage = $"No menu is available for {date:dddd, MMMM dd, yyyy}. Please check another date.";
                return Page();
            }

            Menu = menuDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving menu for date {Date}", date);
            
            Menu = new DailyMenuDto
            {
                MenuDate = date.Date,
                Status = "inactive",
                MenuMeals = new List<MenuMealDto>()
            };
            
            ErrorMessage = $"An error occurred while loading the menu for {date:dddd, MMMM dd, yyyy}. Please try again later.";
            return Page();
        }
    }
}
