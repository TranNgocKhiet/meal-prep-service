using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class RetryModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<RetryModel> _logger;

    public RetryModel(IOrderService orderService, ILogger<RetryModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
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

            // Check if order can be retried
            if (!orderDto.Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "This order cannot be retried at this time.";
                return RedirectToPage("/Order/Details", new { id });
            }

            return RedirectToPage("/Order/Payment", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrying payment for order {OrderId}", id);
            TempData["ErrorMessage"] = "An error occurred while retrying the payment.";
            return RedirectToPage("/Order/Details", new { id });
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
