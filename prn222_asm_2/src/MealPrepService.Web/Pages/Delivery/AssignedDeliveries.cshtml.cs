using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

using System.Security.Claims;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "DeliveryMan")]
public class AssignedDeliveriesModel : PageModel
{
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<AssignedDeliveriesModel> _logger;

    public AssignedDeliveriesModel(IDeliveryService deliveryService, ILogger<AssignedDeliveriesModel> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public List<DeliveryScheduleDto> Deliveries { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);
            
            Deliveries = deliveries.OrderBy(d => d.DeliveryTime).ToList();
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving assigned deliveries for delivery man {DeliveryManId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading your assigned deliveries.";
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
