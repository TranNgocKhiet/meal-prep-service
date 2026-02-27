using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.Pages.Order;

[AllowAnonymous]
public class VnpayCallbackModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<VnpayCallbackModel> _logger;

    public VnpayCallbackModel(IOrderService orderService, ILogger<VnpayCallbackModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync([FromQuery] VnpayCallbackDto callbackDto)
    {
        try
        {
            var order = await _orderService.ProcessVnpayCallbackAsync(callbackDto);
            
            if (order.Status == "confirmed")
            {
                _logger.LogInformation("VNPAY payment successful for order {OrderId}", order.Id);
                return RedirectToPage("/Order/Confirmation", new { id = order.Id });
            }
            else
            {
                _logger.LogWarning("VNPAY payment failed for order {OrderId}", order.Id);
                return RedirectToPage("/Order/PaymentFailed", new { id = order.Id });
            }
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Business error processing VNPAY callback");
            return RedirectToPage("/Order/PaymentError", new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing VNPAY callback");
            return RedirectToPage("/Order/PaymentError", new { message = "An unexpected error occurred while processing your payment." });
        }
    }
}
