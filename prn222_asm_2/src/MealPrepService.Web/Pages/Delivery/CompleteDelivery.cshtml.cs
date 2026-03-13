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

    [BindProperty]
    public Guid DeliveryId { get; set; }

    [BindProperty]
    public string DeliveryResult { get; set; } = "customer_received";

    public CompleteDeliveryModel(IDeliveryService deliveryService, ILogger<CompleteDeliveryModel> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public IActionResult OnGet(Guid deliveryId)
    {
        DeliveryId = deliveryId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await _deliveryService.CompleteDeliveryAsync(DeliveryId, GetCurrentAccountId(), DeliveryResult);
            
            TempData["SuccessMessage"] = "Delivery result saved successfully.";
            _logger.LogInformation("Delivery {DeliveryId} completed by delivery man {DeliveryManId}", 
                DeliveryId, GetCurrentAccountId());
            
            return RedirectToPage("/Delivery/AssignedDeliveries");
        }
        catch (BusinessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Business error completing delivery {DeliveryId}", DeliveryId);
            return Page();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while completing the delivery. Please try again.";
            _logger.LogError(ex, "Unexpected error completing delivery {DeliveryId}", DeliveryId);
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
