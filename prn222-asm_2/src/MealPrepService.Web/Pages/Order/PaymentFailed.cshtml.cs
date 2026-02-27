using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class PaymentFailedModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<PaymentFailedModel> _logger;

    public PaymentFailedModel(IOrderService orderService, ILogger<PaymentFailedModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public OrderDto OrderInfo { get; set; }

    // Helper properties for view binding
    public Guid OrderId => OrderInfo?.Id ?? Guid.Empty;
    public DateTime OrderDate => OrderInfo?.OrderDate ?? DateTime.MinValue;
    public List<OrderDetailDto> OrderDetails => OrderInfo?.OrderDetails ?? new List<OrderDetailDto>();
    public decimal TotalAmount => OrderInfo?.TotalAmount ?? 0;

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
                return Forbid("You don't have permission to view this order.");
            }

            OrderInfo = orderDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading payment failed page for order {OrderId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the payment information.";
            return RedirectToPage("/Order/Index");
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
