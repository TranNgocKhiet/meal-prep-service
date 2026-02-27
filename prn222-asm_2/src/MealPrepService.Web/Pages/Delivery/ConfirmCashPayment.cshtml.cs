using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "DeliveryMan")]
public class ConfirmCashPaymentModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<ConfirmCashPaymentModel> _logger;

    public ConfirmCashPaymentModel(IOrderService orderService, ILogger<ConfirmCashPaymentModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid orderId)
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            var order = await _orderService.ConfirmCashPaymentAsync(orderId, deliveryManId);
            
            TempData["SuccessMessage"] = "Cash payment confirmed successfully.";
            _logger.LogInformation("Cash payment confirmed for order {OrderId} by delivery man {DeliveryManId}", 
                orderId, deliveryManId);
            
            return RedirectToPage("/Delivery/AssignedDeliveries");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Business error confirming cash payment for order {OrderId}", orderId);
            return RedirectToPage("/Delivery/AssignedDeliveries");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while confirming the cash payment. Please try again.";
            _logger.LogError(ex, "Unexpected error confirming cash payment for order {OrderId}", orderId);
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
