using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.Web.PresentationLayer.Cart;


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

    public async Task<IActionResult> OnPostAddToCartAsync(Guid menuMealId, int quantity = 1)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Account/Login", new { returnUrl = Url.Page("/PublicMenu/Today") });
        }

        if (!User.IsInRole("Customer"))
        {
            TempData["ErrorMessage"] = "Only customers can add items to cart.";
            return RedirectToPage();
        }

        try
        {
            var menuDto = await _menuService.GetByDateAsync(DateTime.Today);
            var meal = menuDto?.MenuMeals.FirstOrDefault(x => x.Id == menuMealId && !x.IsSoldOut);

            if (meal == null)
            {
                TempData["ErrorMessage"] = "Selected meal is no longer available.";
                return RedirectToPage();
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
            _logger.LogError(ex, "Failed adding menu meal {MenuMealId} to cart", menuMealId);
            TempData["ErrorMessage"] = "Could not add this item to cart.";
        }

        return RedirectToPage();
    }
}
