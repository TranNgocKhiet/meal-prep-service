using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "DeliveryMan")]
public class AcceptDeliveryModel : PageModel
{
    private readonly IDeliveryService _deliveryService;
    private readonly IHubContext<DeliveryHub> _deliveryHubContext;
    private readonly IHubContext<OrderHub> _orderHubContext;
    private readonly ILogger<AcceptDeliveryModel> _logger;

    public AcceptDeliveryModel(
        IDeliveryService deliveryService,
        IHubContext<DeliveryHub> deliveryHubContext,
        IHubContext<OrderHub> orderHubContext,
        ILogger<AcceptDeliveryModel> logger)
    {
        _deliveryService = deliveryService;
        _deliveryHubContext = deliveryHubContext;
        _orderHubContext = orderHubContext;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid deliveryId)
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            await _deliveryService.AcceptDeliveryAsync(deliveryId, deliveryManId);

            var updatedDelivery = (await _deliveryService.GetByDeliveryManAsync(deliveryManId))
                .FirstOrDefault(d => d.Id == deliveryId);

            if (updatedDelivery != null && updatedDelivery.OrderId != Guid.Empty)
            {
                const string newStatus = "delivering";
                const string message = "Delivery accepted and moved to delivering.";

                await _deliveryHubContext.Clients.All.SendAsync(
                    "ReceiveDeliveryUpdate",
                    deliveryId.ToString(),
                    newStatus,
                    string.Empty,
                    message,
                    updatedDelivery.OrderId.ToString());

                await _orderHubContext.Clients.All.SendAsync(
                    "ReceiveOrderStatusUpdate",
                    updatedDelivery.OrderId.ToString(),
                    newStatus,
                    message);
            }

            TempData["SuccessMessage"] = "Delivery accepted. Order is now in delivering state.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting delivery {DeliveryId}", deliveryId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage("/Delivery/AssignedDeliveries");
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new InvalidOperationException("Authenticated account ID was not found.");
        }

        return accountId;
    }
}
