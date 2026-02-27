using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;


namespace MealPrepService.Web.Pages.PublicMenu;

[AllowAnonymous]
public class TodayModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly ILogger<TodayModel> _logger;

    public TodayModel(IMenuService menuService, ILogger<TodayModel> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public DailyMenuDto Menu { get; set; }
    public string NoMenuMessage { get; set; }
    public string ErrorMessage { get; set; }

    // Helper properties for view binding
    public DateTime MenuDate => Menu?.MenuDate ?? DateTime.Today;
    public List<MenuMealDto> AvailableMeals => Menu?.AvailableMeals ?? new List<MenuMealDto>();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var today = DateTime.Today;
            var menuDto = await _menuService.GetByDateAsync(today);
            
            if (menuDto == null || !menuDto.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                Menu = new DailyMenuDto
                {
                    MenuDate = today,
                    Status = "inactive",
                    MenuMeals = new List<MenuMealDto>()
                };
                
                NoMenuMessage = "No menu is available for today. Please check back later.";
                return Page();
            }

            Menu = menuDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving today's menu");
            
            Menu = new DailyMenuDto
            {
                MenuDate = DateTime.Today,
                Status = "inactive",
                MenuMeals = new List<MenuMealDto>()
            };
            
            ErrorMessage = "An error occurred while loading today's menu. Please try again later.";
            return Page();
        }
    }
}
