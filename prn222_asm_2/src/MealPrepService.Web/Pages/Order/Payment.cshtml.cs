using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class PaymentModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IVnpayService _vnpayService;
    private readonly ILogger<PaymentModel> _logger;

    public PaymentModel(IOrderService orderService, IVnpayService vnpayService, ILogger<PaymentModel> logger)
    {
        _orderService = orderService;
        _vnpayService = vnpayService;
        _logger = logger;
    }

    [BindProperty]
    public Guid OrderId { get; set; }
    
    [BindProperty]
    public string PaymentMethod { get; set; }
    
    [BindProperty]
    public string DeliveryAddress { get; set; }

    [BindProperty]
    public string DeliveryPhone { get; set; }
    
    [BindProperty]
    public DateTime? PreferredDeliveryTime { get; set; }
    
    [BindProperty]
    public decimal OrderTotal { get; set; }
    public List<OrderDetailDto> OrderDetails { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var orderDto = await _orderService.GetByIdAsync(id);
            
            if (orderDto == null)
            {
                return NotFound("Order not found.");
            }

            // Check if user owns this order
            var accountId = GetCurrentAccountId();
            if (orderDto.AccountId != accountId)
            {
                return Forbid("You don't have permission to access this order.");
            }

            // Check if order can be paid
            if (!orderDto.Status.Equals("pending", StringComparison.OrdinalIgnoreCase) && 
                !orderDto.Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "This order cannot be paid at this time.";
                return RedirectToPage("/Order/Details", new { id });
            }

            OrderId = id;
            OrderTotal = orderDto.TotalAmount;
            PaymentMethod = string.IsNullOrWhiteSpace(orderDto.PaymentMethod) ? "COD" : orderDto.PaymentMethod;
            OrderDetails = orderDto.OrderDetails.ToList();
            DeliveryAddress = TempData["DeliveryAddress"]?.ToString()
                ?? orderDto.DeliveryAddress
                ?? string.Empty;
            DeliveryPhone = TempData["DeliveryPhone"]?.ToString()
                ?? orderDto.CustomerContact
                ?? string.Empty;
            PreferredDeliveryTime = TempData["PreferredDeliveryTime"]?.ToString() is string timeStr && DateTime.TryParse(timeStr, out var parsedTime)
                ? parsedTime
                : orderDto.DeliveryTime;

            // Keep values for the next request in case user refreshes before submitting.
            TempData.Keep("DeliveryAddress");
            TempData.Keep("DeliveryPhone");
            TempData.Keep("PreferredDeliveryTime");
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading payment form for order {OrderId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the payment form.";
            return RedirectToPage("/Order/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var accountId = GetCurrentAccountId();
        var orderDto = await _orderService.GetByIdAsync(OrderId);
        if (orderDto.AccountId != accountId)
        {
            return Forbid("You don't have permission to access this order.");
        }

        if (!orderDto.Status.Equals("pending", StringComparison.OrdinalIgnoreCase) &&
            !orderDto.Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "This order cannot be paid at this time.";
            return RedirectToPage("/Order/Details", new { id = OrderId });
        }

        ValidatePaymentForm();

        if (!ModelState.IsValid)
        {
            LoadOrderSnapshot(orderDto);
            return Page();
        }

        try
        {
            OrderTotal = orderDto.TotalAmount;

            if (PaymentMethod == "VNPAY")
            {
                // For VNPAY, redirect to payment gateway
                var order = await _orderService.ProcessPaymentAsync(OrderId, PaymentMethod, DeliveryAddress, PreferredDeliveryTime, DeliveryPhone);
                var paymentUrl = await _vnpayService.CreatePaymentUrlAsync(
                    OrderId, 
                    order.TotalAmount,
                    $"Payment for Order {OrderId}");
                
                _logger.LogInformation("Redirecting to VNPAY for order {OrderId}", OrderId);
                return Redirect(paymentUrl.PaymentUrl);
            }
            else if (PaymentMethod == "COD")
            {
                // For COD, process immediately and show confirmation
                var order = await _orderService.ProcessPaymentAsync(OrderId, PaymentMethod, DeliveryAddress, PreferredDeliveryTime, DeliveryPhone);
                
                _logger.LogInformation("COD order {OrderId} processed successfully", OrderId);
                return RedirectToPage("/Order/Confirmation", new { id = order.Id });
            }
            else
            {
                ModelState.AddModelError("", "Invalid payment method selected.");
                LoadOrderSnapshot(orderDto);
                return Page();
            }
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            LoadOrderSnapshot(orderDto);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing payment for order {OrderId}", OrderId);
            ModelState.AddModelError(string.Empty, "An error occurred while processing the payment. Please try again.");

            LoadOrderSnapshot(orderDto);
            return Page();
        }
    }

    private void LoadOrderSnapshot(OrderDto orderDto)
    {
        OrderTotal = orderDto.TotalAmount;
        OrderDetails = orderDto.OrderDetails.ToList();

        if (string.IsNullOrWhiteSpace(PaymentMethod))
        {
            PaymentMethod = string.IsNullOrWhiteSpace(orderDto.PaymentMethod) ? "COD" : orderDto.PaymentMethod;
        }
    }

    private void ValidatePaymentForm()
    {
        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            ModelState.AddModelError(nameof(DeliveryAddress), "Delivery address is required.");
        }

        if (string.IsNullOrWhiteSpace(DeliveryPhone))
        {
            ModelState.AddModelError(nameof(DeliveryPhone), "Delivery phone number is required.");
        }
        else
        {
            var normalizedPhone = DeliveryPhone.Trim();
            if (normalizedPhone.Length < 8 || normalizedPhone.Length > 20)
            {
                ModelState.AddModelError(nameof(DeliveryPhone), "Delivery phone number must be between 8 and 20 characters.");
            }
            else
            {
                DeliveryPhone = normalizedPhone;
            }
        }

        if (!PreferredDeliveryTime.HasValue)
        {
            ModelState.AddModelError(nameof(PreferredDeliveryTime), "Preferred delivery time is required.");
        }
        else if (PreferredDeliveryTime.Value <= DateTime.Now)
        {
            ModelState.AddModelError(nameof(PreferredDeliveryTime), "Preferred delivery time must be in the future.");
        }

        if (!string.Equals(PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(PaymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(PaymentMethod), "Invalid payment method selected.");
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
}
