using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.Web.PresentationLayer.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class MyCartModel : PageModel
{
    private readonly IMenuService _menuService;
    private readonly IOrderService _orderService;
    private readonly IVnpayService _vnpayService;
    private readonly ILogger<MyCartModel> _logger;

    public MyCartModel(
        IMenuService menuService,
        IOrderService orderService,
        IVnpayService vnpayService,
        ILogger<MyCartModel> logger)
    {
        _menuService = menuService;
        _orderService = orderService;
        _vnpayService = vnpayService;
        _logger = logger;
    }

    [BindProperty]
    public string DeliveryAddress { get; set; } = string.Empty;

    [BindProperty]
    public string DeliveryPhone { get; set; } = string.Empty;

    [BindProperty]
    public DateTime PreferredDeliveryTime { get; set; } = DateTime.Now.AddDays(1).Date.AddHours(18);

    [BindProperty]
    public string PaymentMethod { get; set; } = "COD";

    public List<CartDisplayItem> CartItems { get; set; } = new();
    public decimal TotalAmount => CartItems.Sum(x => x.Subtotal);

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadCartAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid menuMealId, int quantity)
    {
        var cart = HttpContext.Session.GetCartItems();
        var existing = cart.FirstOrDefault(x => x.MenuMealId == menuMealId);

        if (existing != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(existing);
            }
            else
            {
                existing.Quantity = quantity;
            }

            HttpContext.Session.SaveCartItems(cart);
        }

        return RedirectToPage();
    }

    public IActionResult OnPostRemove(Guid menuMealId)
    {
        var cart = HttpContext.Session.GetCartItems();
        cart.RemoveAll(x => x.MenuMealId == menuMealId);
        HttpContext.Session.SaveCartItems(cart);
        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        HttpContext.Session.ClearCart();
        TempData["SuccessMessage"] = "Cart cleared.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            ModelState.AddModelError(string.Empty, "Delivery address is required.");
            await LoadCartAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(DeliveryPhone))
        {
            ModelState.AddModelError(string.Empty, "Delivery phone number is required.");
            await LoadCartAsync();
            return Page();
        }

        if (DeliveryPhone.Trim().Length < 8 || DeliveryPhone.Trim().Length > 20)
        {
            ModelState.AddModelError(string.Empty, "Delivery phone number must be between 8 and 20 characters.");
            await LoadCartAsync();
            return Page();
        }

        if (PreferredDeliveryTime <= DateTime.Now)
        {
            ModelState.AddModelError(string.Empty, "Preferred delivery time must be in the future.");
            await LoadCartAsync();
            return Page();
        }

        var cart = HttpContext.Session.GetCartItems();
        if (!cart.Any())
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
            await LoadCartAsync();
            return Page();
        }

        try
        {
            var accountId = GetCurrentAccountId();
            var orderItems = cart
                .Where(x => x.Quantity > 0)
                .Select(x => new OrderItemDto
                {
                    MenuMealId = x.MenuMealId,
                    Quantity = x.Quantity
                })
                .ToList();

            var order = await _orderService.CreateOrderAsync(accountId, orderItems);
            var paidOrder = await _orderService.ProcessPaymentAsync(
                order.Id,
                PaymentMethod,
                DeliveryAddress,
                PreferredDeliveryTime,
                DeliveryPhone);

            HttpContext.Session.ClearCart();

            if (string.Equals(PaymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase))
            {
                var paymentUrl = await _vnpayService.CreatePaymentUrlAsync(
                    paidOrder.Id,
                    paidOrder.TotalAmount,
                    $"Payment for Order {paidOrder.Id}");

                return Redirect(paymentUrl.PaymentUrl);
            }

            TempData["SuccessMessage"] = "Order placed successfully. Payment is pending and waiting for staff confirmation.";
            return RedirectToPage("/Order/Confirmation", new { id = paidOrder.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadCartAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking out cart for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while placing your order.");
            await LoadCartAsync();
            return Page();
        }
    }

    private async Task LoadCartAsync()
    {
        CartItems = new List<CartDisplayItem>();
        var cart = HttpContext.Session.GetCartItems();

        if (!cart.Any())
        {
            return;
        }

        var menu = await _menuService.GetByDateAsync(DateTime.Today);
        if (menu == null)
        {
            return;
        }

        foreach (var line in cart)
        {
            var meal = menu.MenuMeals.FirstOrDefault(x => x.Id == line.MenuMealId);
            if (meal == null)
            {
                continue;
            }

            CartItems.Add(new CartDisplayItem
            {
                MenuMealId = meal.Id,
                RecipeName = meal.RecipeName,
                UnitPrice = meal.Price,
                AvailableQuantity = meal.AvailableQuantity,
                Quantity = Math.Min(line.Quantity, meal.AvailableQuantity)
            });
        }

        // Keep cart synchronized with stock constraints.
        var normalizedCart = CartItems
            .Where(x => x.Quantity > 0)
            .Select(x => new CartItemSession { MenuMealId = x.MenuMealId, Quantity = x.Quantity })
            .ToList();

        HttpContext.Session.SaveCartItems(normalizedCart);
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new AuthenticationException("User account ID not found in claims.");
        }

        return accountId;
    }

    public sealed class CartDisplayItem
    {
        public Guid MenuMealId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
