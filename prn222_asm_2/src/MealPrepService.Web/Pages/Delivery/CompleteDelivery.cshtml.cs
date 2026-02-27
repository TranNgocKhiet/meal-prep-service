using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "DeliveryMan")]
public class CompleteDeliveryModel : PageModel
{
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<CompleteDeliveryModel> _logger;

    public CompleteDeliveryModel(IDeliveryService deliveryService, ILogger<CompleteDeliveryModel> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid deliveryId)
    {
        try
        {
            await _deliveryService.CompleteDeliveryAsync(deliveryId);
            
            TempData["SuccessMessage"] = "Delivery completed successfully.";
            _logger.LogInformation("Delivery {DeliveryId} completed by delivery man {DeliveryManId}", 
                deliveryId, GetCurrentAccountId());
            
            return RedirectToPage("/Delivery/AssignedDeliveries");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Business error completing delivery {DeliveryId}", deliveryId);
            return RedirectToPage("/Delivery/AssignedDeliveries");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while completing the delivery. Please try again.";
            _logger.LogError(ex, "Unexpected error completing delivery {DeliveryId}", deliveryId);
            return RedirectToPage("/Delivery/AssignedDeliveries");
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
