using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.Hubs;

using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "DeliveryMan")]
public class AssignedDeliveriesModel : PageModel
{
    private const int CompletedPageSize = 8;

    private readonly IDeliveryService _deliveryService;
    private readonly IHubContext<DeliveryHub> _deliveryHubContext;
    private readonly IHubContext<OrderHub> _orderHubContext;
    private readonly ILogger<AssignedDeliveriesModel> _logger;

    public AssignedDeliveriesModel(
        IDeliveryService deliveryService,
        IHubContext<DeliveryHub> deliveryHubContext,
        IHubContext<OrderHub> orderHubContext,
        ILogger<AssignedDeliveriesModel> logger)
    {
        _deliveryService = deliveryService;
        _deliveryHubContext = deliveryHubContext;
        _orderHubContext = orderHubContext;
        _logger = logger;
    }

    public List<DeliveryScheduleDto> Deliveries { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "delivering";

    [BindProperty(SupportsGet = true)]
    public string CompletedStatus { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public DateTime? CompletedDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int DeliveringCount { get; set; }
    public int CompletedCount { get; set; }
    public int CompletedFilteredCount { get; set; }
    public int PageSize => CompletedPageSize;
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var deliveryManId = GetCurrentAccountId();
            var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);

            Tab = NormalizeTab(Tab);
            CompletedStatus = NormalizeCompletedStatus(CompletedStatus);
            PageNumber = PageNumber < 1 ? 1 : PageNumber;

            var allDeliveries = deliveries.OrderBy(d => d.DeliveryTime).ToList();

            DeliveringCount = allDeliveries.Count(IsDelivering);
            CompletedCount = allDeliveries.Count(IsCompleted);

            if (Tab == "completed")
            {
                var completedQuery = allDeliveries
                    .Where(IsCompleted)
                    .Where(MatchesCompletedStatus)
                    .Where(MatchesCompletedDate)
                    .OrderByDescending(d => d.DeliveryTime)
                    .ToList();

                CompletedFilteredCount = completedQuery.Count;
                TotalPages = CompletedFilteredCount == 0
                    ? 1
                    : (int)Math.Ceiling(CompletedFilteredCount / (double)CompletedPageSize);

                if (PageNumber > TotalPages)
                {
                    PageNumber = TotalPages;
                }

                Deliveries = completedQuery
                    .Skip((PageNumber - 1) * CompletedPageSize)
                    .Take(CompletedPageSize)
                    .ToList();
            }
            else
            {
                TotalPages = 1;
                PageNumber = 1;
                CompletedFilteredCount = CompletedCount;

                Deliveries = allDeliveries
                    .Where(IsDelivering)
                    .OrderBy(d => d.DeliveryTime)
                    .ToList();
            }
            
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
            var currentDeliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);
            var targetDelivery = currentDeliveries.FirstOrDefault(d => d.Id == deliveryId);

            if (targetDelivery == null)
            {
                TempData["ErrorMessage"] = "Delivery not found or not assigned to your account.";
                return RedirectToPage(new
                {
                    tab = "completed",
                    completedStatus = CompletedStatus,
                    completedDate = CompletedDate?.ToString("yyyy-MM-dd"),
                    pageNumber = PageNumber
                });
            }

            if (!CanEditDeliveryResult(targetDelivery))
            {
                TempData["ErrorMessage"] = "Delivery result cannot be changed for the current order status.";
                return RedirectToPage(new
                {
                    tab = "completed",
                    completedStatus = CompletedStatus,
                    completedDate = CompletedDate?.ToString("yyyy-MM-dd"),
                    pageNumber = PageNumber
                });
            }

            await _deliveryService.CompleteDeliveryAsync(deliveryId, deliveryManId, resultStatus);
            if (targetDelivery.OrderId != Guid.Empty)
            {
                var message = $"Delivery status updated to {resultStatus}.";
                await _deliveryHubContext.Clients.All.SendAsync(
                    "ReceiveDeliveryUpdate",
                    deliveryId.ToString(),
                    resultStatus,
                    string.Empty,
                    message,
                    targetDelivery.OrderId.ToString());

                await _orderHubContext.Clients.All.SendAsync(
                    "ReceiveOrderStatusUpdate",
                    targetDelivery.OrderId.ToString(),
                    resultStatus,
                    message);
            }
            TempData["SuccessMessage"] = "Delivery result saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing delivery {DeliveryId}", deliveryId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new
        {
            tab = Tab,
            completedStatus = CompletedStatus,
            completedDate = CompletedDate?.ToString("yyyy-MM-dd"),
            pageNumber = PageNumber
        });
    }

    private static string NormalizeTab(string? tab)
    {
        var normalized = (tab ?? string.Empty).Trim().ToLowerInvariant();
        return normalized == "completed" ? "completed" : "delivering";
    }

    private static string NormalizeCompletedStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();

        return normalized == "customer_received"
               || normalized == "customer_reject"
               || normalized == "failed"
            ? normalized
            : "all";
    }

    private bool MatchesCompletedStatus(DeliveryScheduleDto delivery)
    {
        return CompletedStatus == "all"
               || delivery.OrderStatus.Equals(CompletedStatus, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesCompletedDate(DeliveryScheduleDto delivery)
    {
        return !CompletedDate.HasValue || delivery.DeliveryTime.Date == CompletedDate.Value.Date;
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

    private static bool CanEditDeliveryResult(DeliveryScheduleDto delivery)
    {
        return IsDelivering(delivery) || IsCompleted(delivery);
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
