using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "DeliveryMan")]
public class UpdateTimeModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<UpdateTimeModel> _logger;

    [BindProperty]
    public Guid DeliveryId { get; set; }

    [BindProperty]
    public Guid OrderId { get; set; }

    [BindProperty]
    public DateTime CurrentDeliveryTime { get; set; }

    [BindProperty]
    public DateTime NewDeliveryTime { get; set; }

    [BindProperty]
    public string Address { get; set; } = string.Empty;

    public UpdateTimeModel(
        IOrderService orderService, 
        IDeliveryService deliveryService,
        ILogger<UpdateTimeModel> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync(Guid deliveryId)
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);
            var delivery = deliveries.FirstOrDefault(d => d.Id == deliveryId);
            
            if (delivery == null)
            {
                TempData["ErrorMessage"] = "Delivery not found or you don't have permission to update it.";
                return RedirectToPage("/Delivery/AssignedDeliveries");
            }
            
            DeliveryId = delivery.Id;
            OrderId = delivery.OrderId;
            CurrentDeliveryTime = delivery.DeliveryTime;
            NewDeliveryTime = delivery.DeliveryTime.AddHours(1);
            Address = delivery.Address;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading update delivery time form for delivery {DeliveryId}", deliveryId);
            TempData["ErrorMessage"] = "An error occurred while loading the update form.";
            return RedirectToPage("/Delivery/AssignedDeliveries");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        try
        {
            await _deliveryService.UpdateDeliveryTimeAsync(DeliveryId, NewDeliveryTime);
            
            TempData["SuccessMessage"] = "Delivery time updated successfully.";
            _logger.LogInformation("Delivery time updated for delivery {DeliveryId} to {NewTime} by delivery man {DeliveryManId}", 
                DeliveryId, NewDeliveryTime, GetCurrentAccountId());
            
            return RedirectToPage("/Delivery/AssignedDeliveries");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError("", ex.Message);
            _logger.LogWarning(ex, "Business error updating delivery time for delivery {DeliveryId}", DeliveryId);
            return Page();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while updating the delivery time. Please try again.");
            _logger.LogError(ex, "Unexpected error updating delivery time for delivery {DeliveryId}", DeliveryId);
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
