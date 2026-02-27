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
    public DateTime? PreferredDeliveryTime { get; set; }
    
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
            PaymentMethod = "Credit Card";
            OrderDetails = orderDto.OrderDetails.ToList();
            DeliveryAddress = TempData["DeliveryAddress"]?.ToString() ?? "Not specified";
            PreferredDeliveryTime = TempData["PreferredDeliveryTime"]?.ToString() is string timeStr && DateTime.TryParse(timeStr, out var parsedTime)
                ? parsedTime
                : null;
            
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
        if (!ModelState.IsValid)
        {
            // Reload order details for the form
            var orderDto = await _orderService.GetByIdAsync(OrderId);
            if (orderDto != null)
            {
                OrderDetails = orderDto.OrderDetails.ToList();
            }
            return Page();
        }

        try
        {
            if (PaymentMethod == "VNPAY")
            {
                // For VNPAY, redirect to payment gateway
                var order = await _orderService.ProcessPaymentAsync(OrderId, PaymentMethod);
                var paymentUrl = await _vnpayService.CreatePaymentUrlAsync(
                    OrderId, 
                    OrderTotal, 
                    $"Payment for Order {OrderId}");
                
                _logger.LogInformation("Redirecting to VNPAY for order {OrderId}", OrderId);
                return Redirect(paymentUrl.PaymentUrl);
            }
            else if (PaymentMethod == "COD")
            {
                // For COD, process immediately and show confirmation
                var order = await _orderService.ProcessPaymentAsync(OrderId, PaymentMethod);
                
                _logger.LogInformation("COD order {OrderId} processed successfully", OrderId);
                return RedirectToPage("/Order/Confirmation", new { id = order.Id });
            }
            else
            {
                ModelState.AddModelError("", "Invalid payment method selected.");
                
                // Reload order details for the form
                var orderDto = await _orderService.GetByIdAsync(OrderId);
                if (orderDto != null)
                {
                    OrderDetails = orderDto.OrderDetails.ToList();
                }
                
                return Page();
            }
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            
            // Reload order details for the form
            var orderDto = await _orderService.GetByIdAsync(OrderId);
            if (orderDto != null)
            {
                OrderDetails = orderDto.OrderDetails.ToList();
            }
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing payment for order {OrderId}", OrderId);
            ModelState.AddModelError(string.Empty, "An error occurred while processing the payment. Please try again.");
            
            // Reload order details for the form
            var orderDto = await _orderService.GetByIdAsync(OrderId);
            if (orderDto != null)
            {
                OrderDetails = orderDto.OrderDetails.ToList();
            }
            
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
}
