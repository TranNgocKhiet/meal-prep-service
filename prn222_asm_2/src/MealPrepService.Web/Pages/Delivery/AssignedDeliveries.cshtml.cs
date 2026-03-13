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

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "delivering";

    public int DeliveringCount { get; set; }
    public int CompletedCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);

            Tab = NormalizeTab(Tab);
            var allDeliveries = deliveries.OrderBy(d => d.DeliveryTime).ToList();
            DeliveringCount = allDeliveries.Count(IsDelivering);
            CompletedCount = allDeliveries.Count(IsCompleted);

            Deliveries = Tab == "completed"
                ? allDeliveries.Where(IsCompleted).OrderByDescending(d => d.DeliveryTime).ToList()
                : allDeliveries.Where(IsDelivering).OrderBy(d => d.DeliveryTime).ToList();
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving assigned deliveries for delivery man {DeliveryManId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading your assigned deliveries.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCompleteAsync(Guid deliveryId, string resultStatus)
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            await _deliveryService.CompleteDeliveryAsync(deliveryId, deliveryManId, resultStatus);
            TempData["SuccessMessage"] = "Delivery result saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing delivery {DeliveryId}", deliveryId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { tab = Tab });
    }

    private static string NormalizeTab(string? tab)
    {
        var normalized = (tab ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "completed" ? "completed" : "delivering";
    }

    private static bool IsDelivering(DeliveryScheduleDto delivery)
    {
        return delivery.OrderStatus.Equals("delivering", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCompleted(DeliveryScheduleDto delivery)
    {
        return delivery.OrderStatus.Equals("customer_received", StringComparison.OrdinalIgnoreCase)
               || delivery.OrderStatus.Equals("customer_reject", StringComparison.OrdinalIgnoreCase)
               || delivery.OrderStatus.Equals("failed", StringComparison.OrdinalIgnoreCase);
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
