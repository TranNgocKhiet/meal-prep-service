using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class DetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IOrderService orderService, ILogger<DetailsModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public OrderDto OrderDetails { get; set; }

    // Helper properties for view
    public Guid Id => OrderDetails?.Id ?? Guid.Empty;
    public string Status => OrderDetails?.Status ?? string.Empty;
    public DateTime OrderDate => OrderDetails?.OrderDate ?? DateTime.MinValue;
    public string PaymentMethod => OrderDetails?.PaymentMethod ?? string.Empty;
    public decimal TotalAmount => OrderDetails?.TotalAmount ?? 0;
    public List<OrderDetailDto> OrderDetails_Items => OrderDetails?.OrderDetails ?? new List<OrderDetailDto>();
    public DeliveryScheduleDto? DeliveryInfo => OrderDetails?.DeliverySchedule;

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

            OrderDetails = orderDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving order {OrderId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the order details.";
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
