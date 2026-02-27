using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

using System.Security.Claims;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "Customer")]
public class MyDeliveriesModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<MyDeliveriesModel> _logger;

    public MyDeliveriesModel(IOrderService orderService, IDeliveryService deliveryService, ILogger<MyDeliveriesModel> logger)
    {
        _orderService = orderService;
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public List<DeliveryScheduleDto> UpcomingDeliveries { get; set; } = new();
    public List<DeliveryScheduleDto> CompletedDeliveries { get; set; } = new();

    // Helper properties for statistics
    public int TotalUpcoming => UpcomingDeliveries.Count;
    public int TotalCompleted => CompletedDeliveries.Count;
    public int UrgentCount => UpcomingDeliveries.Count(d => d.IsOverdue || (d.DeliveryTime - DateTime.Now).TotalHours < 2);
    public int OverdueCount => UpcomingDeliveries.Count(d => d.IsOverdue);
    public bool HasUpcomingDeliveries => UpcomingDeliveries.Any();
    public bool HasOverdueDeliveries => OverdueCount > 0;
    public bool HasCompletedDeliveries => CompletedDeliveries.Any();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var customerId = GetCurrentAccountId();
            var deliveries = await _deliveryService.GetByAccountIdAsync(customerId);
            
            UpcomingDeliveries = deliveries
                .Where(d => d.Order?.Status != "delivered")
                .OrderBy(d => d.DeliveryTime)
                .ToList();
            
            CompletedDeliveries = deliveries
                .Where(d => d.Order?.Status == "delivered")
                .OrderByDescending(d => d.DeliveryTime)
                .ToList();
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving deliveries for customer {CustomerId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading your deliveries.";
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
