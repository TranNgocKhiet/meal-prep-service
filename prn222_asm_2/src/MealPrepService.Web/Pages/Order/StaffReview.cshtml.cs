using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Admin,Manager")]
public class StaffReviewModel : PageModel
{
    private static readonly string[] StatusTabs =
    {
        "pending",
        "confirmed",
        "preparing",
        "preparing_failed",
        "prepared",
        "on_scheduled",
        "cancelled"
    };

    private readonly IOrderService _orderService;
    private readonly IDeliveryService _deliveryService;
    private readonly IAccountService _accountService;
    private readonly ILogger<StaffReviewModel> _logger;

    public StaffReviewModel(IOrderService orderService, IDeliveryService deliveryService, IAccountService accountService, ILogger<StaffReviewModel> logger)
    {
        _orderService = orderService;
        _deliveryService = deliveryService;
        _accountService = accountService;
        _logger = logger;
    }

    public List<OrderDto> Orders { get; set; } = new();
    public Dictionary<string, int> StatusCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SelectListItem> DeliveryManOptions { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "pending";

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "all";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOrdersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(Guid orderId, string? statusFilter, string? periodFilter)
    {
        try
        {
            var staffId = GetCurrentAccountId();
            await _orderService.TransitionOrderForOperationsAsync(orderId, "confirmed", staffId);
            TempData["SuccessMessage"] = "Order confirmed successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order {OrderId}", orderId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { status = NormalizeTab(statusFilter), period = NormalizePeriod(periodFilter) });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid orderId, string? statusFilter, string? periodFilter)
    {
        try
        {
            var staffId = GetCurrentAccountId();
            await _orderService.TransitionOrderForOperationsAsync(orderId, "cancelled", staffId);
            TempData["InfoMessage"] = "Order cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { status = NormalizeTab(statusFilter), period = NormalizePeriod(periodFilter) });
    }

    public async Task<IActionResult> OnPostPrepareAsync(Guid orderId, string? statusFilter, string? periodFilter)
    {
        return await TransitionOrderAsync(orderId, "preparing", "Order moved to preparing.", statusFilter, periodFilter);
    }

    public async Task<IActionResult> OnPostPrepareSuccessAsync(Guid orderId, string? statusFilter, string? periodFilter)
    {
        return await TransitionOrderAsync(orderId, "prepared", "Order marked as prepared.", statusFilter, periodFilter);
    }

    public async Task<IActionResult> OnPostPrepareFailedAsync(Guid orderId, string? statusFilter, string? periodFilter)
    {
        return await TransitionOrderAsync(orderId, "preparing_failed", "Order marked as preparing failed.", statusFilter, periodFilter);
    }

    public async Task<IActionResult> OnPostScheduleAsync(Guid orderId, Guid deliveryScheduleId, DateTime selectedDeliveryTime, Guid deliveryManId, string? statusFilter, string? periodFilter)
    {
        try
        {
            var normalizedDeliveryTime = NormalizeToUtc(selectedDeliveryTime);
            await _deliveryService.UpdateDeliveryTimeAsync(deliveryScheduleId, normalizedDeliveryTime);
            await _deliveryService.AssignDeliveryManAsync(deliveryScheduleId, deliveryManId);

            var staffId = GetCurrentAccountId();
            await _orderService.TransitionOrderForOperationsAsync(orderId, "delivering", staffId);
            TempData["SuccessMessage"] = "Order moved to on scheduled and assigned to delivery man.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling order {OrderId} with delivery schedule {DeliveryScheduleId}", orderId, deliveryScheduleId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { status = NormalizeTab(statusFilter), period = NormalizePeriod(periodFilter) });
    }

    public string NormalizeTabStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "pending" or "pending_confirmation" => "pending",
            "confirmed" => "confirmed",
            "preparing" => "preparing",
            "preparing_failed" => "preparing_failed",
            "prepared" => "prepared",
            "delivering" => "on_scheduled",
            "cancelled" => "cancelled",
            _ => string.Empty
        };
    }

    public string GetDisplayStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "pending" or "pending_confirmation" => "OrderPending",
            "confirmed" => "OrderConfirmed",
            "preparing" => "Preparing",
            "preparing_failed" => "PreparingFailed",
            "prepared" => "Prepared",
            "delivering" => "Delivering",
            "cancelled" => "OrderCancelled",
            _ => status ?? "Unknown"
        };
    }

    public string GetBadgeClass(string? status)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "pending" or "pending_confirmation" => "status-badge status-pending",
            "confirmed" or "preparing" or "preparing_failed" => "status-badge status-active",
            "prepared" or "delivering" => "status-badge status-neutral",
            "cancelled" => "status-badge status-cancelled",
            _ => "status-badge status-default"
        };
    }

    public int GetStatusCount(string tabKey)
    {
        return StatusCounts.TryGetValue(tabKey, out var count) ? count : 0;
    }

    private async Task LoadOrdersAsync()
    {
        Status = NormalizeTab(Status);
        Period = NormalizePeriod(Period);

        await LoadDeliveryManOptionsAsync();

        var operationOrders = (await _orderService.GetOperationsOrdersAsync()).ToList();
        var periodFiltered = ApplyPeriodFilter(operationOrders, Period);

        foreach (var tab in StatusTabs)
        {
            StatusCounts[tab] = periodFiltered.Count(o => NormalizeTabStatus(o.Status) == tab);
        }

        Orders = periodFiltered
            .Where(o => NormalizeTabStatus(o.Status) == Status)
            .OrderByDescending(o => o.OrderDate)
            .ToList();
    }

    private async Task LoadDeliveryManOptionsAsync()
    {
        var deliveryMen = await _accountService.GetAccountsByRoleAsync("DeliveryMan");
        DeliveryManOptions = deliveryMen
            .OrderBy(x => x.FullName)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.FullName
            })
            .ToList();
    }

    private static string NormalizeTab(string? tab)
    {
        var normalized = (tab ?? string.Empty).Trim().ToLowerInvariant();
        return StatusTabs.Contains(normalized, StringComparer.OrdinalIgnoreCase) ? normalized : "pending";
    }

    private static string NormalizePeriod(string? period)
    {
        var normalized = (period ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "today" or "this_week" or "this_month" => normalized,
            _ => "all"
        };
    }

    private static List<OrderDto> ApplyPeriodFilter(List<OrderDto> orders, string period)
    {
        var now = DateTime.Now;

        return period switch
        {
            "today" => orders.Where(o => o.OrderDate.ToLocalTime().Date == now.Date).ToList(),
            "this_week" => orders.Where(o =>
            {
                var weekStart = now.Date.AddDays(-((int)now.DayOfWeek + 6) % 7);
                var weekEnd = weekStart.AddDays(7);
                var localDate = o.OrderDate.ToLocalTime();
                return localDate >= weekStart && localDate < weekEnd;
            }).ToList(),
            "this_month" => orders.Where(o =>
            {
                var localDate = o.OrderDate.ToLocalTime();
                return localDate.Year == now.Year && localDate.Month == now.Month;
            }).ToList(),
            _ => orders
        };
    }

    private async Task<IActionResult> TransitionOrderAsync(Guid orderId, string targetStatus, string successMessage, string? statusFilter, string? periodFilter)
    {
        try
        {
            var staffId = GetCurrentAccountId();
            await _orderService.TransitionOrderForOperationsAsync(orderId, targetStatus, staffId);
            TempData["SuccessMessage"] = successMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transitioning order {OrderId} to {Status}", orderId, targetStatus);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { status = NormalizeTab(statusFilter), period = NormalizePeriod(periodFilter) });
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

    private static DateTime NormalizeToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    }
}
