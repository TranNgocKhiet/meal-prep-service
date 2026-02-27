using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class CreateModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IMenuService _menuService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IOrderService orderService, IMenuService menuService, ILogger<CreateModel> logger)
    {
        _orderService = orderService;
        _menuService = menuService;
        _logger = logger;
    }

    [BindProperty]
    public DateTime MenuDate { get; set; }
    
    [BindProperty]
    public string DeliveryAddress { get; set; }
    
    [BindProperty]
    public string DeliveryNotes { get; set; }
    
    [BindProperty]
    public DateTime? PreferredDeliveryTime { get; set; }
    
    [BindProperty]
    public List<OrderItemInput> OrderItems { get; set; } = new();

    public List<MenuMealDto> AvailableMeals { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(DateTime? menuDate)
    {
        try
        {
            var selectedDate = menuDate ?? DateTime.Today;
            var menuDto = await _menuService.GetByDateAsync(selectedDate);
            
            if (menuDto == null || !menuDto.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"No active menu is available for {selectedDate:dddd, MMMM dd, yyyy}.";
                return RedirectToPage("/PublicMenu/Today");
            }

            MenuDate = selectedDate;
            DeliveryAddress = string.Empty;
            PreferredDeliveryTime = DateTime.Now.AddDays(1).Date.AddHours(18); // Default to 6 PM tomorrow
            AvailableMeals = menuDto.MenuMeals.Where(m => !m.IsSoldOut).ToList();
            OrderItems = menuDto.MenuMeals
                .Where(m => !m.IsSoldOut)
                .Select(m => new OrderItemInput
                {
                    MenuMealId = m.Id,
                    RecipeName = m.RecipeName,
                    UnitPrice = m.Price,
                    Quantity = 0,
                    AvailableQuantity = m.AvailableQuantity,
                    Recipe = m.Recipe
                })
                .ToList();
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading create order form for date {MenuDate}", menuDate);
            TempData["ErrorMessage"] = "An error occurred while loading the order form.";
            return RedirectToPage("/PublicMenu/Today");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await ReloadAvailableMeals();
            return Page();
        }

        try
        {
            var accountId = GetCurrentAccountId();
            
            // Filter only selected items
            var selectedItems = OrderItems
                .Where(item => item.Quantity > 0)
                .Select(item => new OrderItemDto
                {
                    MenuMealId = item.MenuMealId,
                    Quantity = item.Quantity
                })
                .ToList();

            if (!selectedItems.Any())
            {
                ModelState.AddModelError(string.Empty, "Please select at least one item to order.");
                await ReloadAvailableMeals();
                return Page();
            }

            var orderDto = await _orderService.CreateOrderAsync(accountId, selectedItems);
            
            _logger.LogInformation("Order {OrderId} created successfully for account {AccountId}", 
                orderDto.Id, accountId);
            
            // Store delivery information in TempData for payment page
            TempData["DeliveryAddress"] = DeliveryAddress;
            TempData["DeliveryNotes"] = DeliveryNotes;
            TempData["PreferredDeliveryTime"] = PreferredDeliveryTime?.ToString("yyyy-MM-dd HH:mm");
            
            return RedirectToPage("/Order/Payment", new { id = orderDto.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await ReloadAvailableMeals();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating order for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while creating the order. Please try again.");
            await ReloadAvailableMeals();
            return Page();
        }
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new AuthenticationException("User account ID not found in claims.");
        }
        return accountId;
    }

    private async Task ReloadAvailableMeals()
    {
        try
        {
            var menuDto = await _menuService.GetByDateAsync(MenuDate);
            if (menuDto != null)
            {
                AvailableMeals = menuDto.MenuMeals.Where(m => !m.IsSoldOut).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reloading available meals for date {MenuDate}", MenuDate);
        }
    }

    public class OrderItemInput
    {
        public Guid MenuMealId { get; set; }
        public string RecipeName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }
        public RecipeDto? Recipe { get; set; }
    }
}
