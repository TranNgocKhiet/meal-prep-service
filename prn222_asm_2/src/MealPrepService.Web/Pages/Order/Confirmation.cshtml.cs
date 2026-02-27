using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class ConfirmationModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<ConfirmationModel> _logger;

    public ConfirmationModel(IOrderService orderService, ILogger<ConfirmationModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public OrderDto OrderConfirmation { get; set; }

    // Helper properties for view
    public Guid OrderId => OrderConfirmation?.Id ?? Guid.Empty;
    public DateTime OrderDate => OrderConfirmation?.OrderDate ?? DateTime.MinValue;
    public string Status => OrderConfirmation?.Status ?? string.Empty;
    public string PaymentMethod => OrderConfirmation?.PaymentMethod ?? string.Empty;
    public decimal TotalAmount => OrderConfirmation?.TotalAmount ?? 0;
    public List<OrderDetailDto> OrderDetails => OrderConfirmation?.OrderDetails ?? new List<OrderDetailDto>();
    public DeliveryScheduleDto? DeliverySchedule => OrderConfirmation?.DeliverySchedule;
    public DeliveryScheduleDto? DeliveryInfo => OrderConfirmation?.DeliverySchedule; // Alias for view compatibility

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

            OrderConfirmation = orderDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading confirmation for order {OrderId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the order confirmation.";
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
