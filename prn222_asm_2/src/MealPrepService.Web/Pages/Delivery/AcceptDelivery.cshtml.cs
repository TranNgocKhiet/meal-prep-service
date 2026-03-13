using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "DeliveryMan")]
public class AcceptDeliveryModel : PageModel
{
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<AcceptDeliveryModel> _logger;

    public AcceptDeliveryModel(IDeliveryService deliveryService, ILogger<AcceptDeliveryModel> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid deliveryId)
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            await _deliveryService.AcceptDeliveryAsync(deliveryId, deliveryManId);
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
