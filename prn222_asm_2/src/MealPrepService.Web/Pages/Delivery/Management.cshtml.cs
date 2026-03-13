using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MealPrepService.Web.Pages.Delivery;

[Authorize(Roles = "Admin,Manager")]
public class ManagementModel : PageModel
{
    private readonly IDeliveryService _deliveryService;
    private readonly IAccountService _accountService;
    private readonly ILogger<ManagementModel> _logger;

    public ManagementModel(IDeliveryService deliveryService, IAccountService accountService, ILogger<ManagementModel> logger)
    {
        _deliveryService = deliveryService;
        _accountService = accountService;
        _logger = logger;
    }

    public List<DeliveryScheduleDto> Deliveries { get; set; } = new();
    public List<SelectListItem> DeliveryManOptions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "all";

    public int AllCount { get; set; }
    public int DeliveringCount { get; set; }
    public int CompletedCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid deliveryId, DateTime selectedDeliveryTime, Guid deliveryManId, string? tab)
    {
        try
        {
            var normalizedTime = NormalizeToUtc(selectedDeliveryTime);
            await _deliveryService.UpdateDeliveryTimeAsync(deliveryId, normalizedTime);
            await _deliveryService.AssignDeliveryManAsync(deliveryId, deliveryManId);
            TempData["SuccessMessage"] = "Delivery schedule updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery schedule {DeliveryId}", deliveryId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { tab = NormalizeTab(tab) });
    }

    private async Task LoadDataAsync()
    {
        Tab = NormalizeTab(Tab);

        var deliveries = (await _deliveryService.GetAllForOperationsAsync()).ToList();
        AllCount = deliveries.Count;
        DeliveringCount = deliveries.Count(IsDelivering);
        CompletedCount = deliveries.Count(IsCompleted);

        Deliveries = Tab switch
        {
            "delivering" => deliveries.Where(IsDelivering).OrderBy(d => d.DeliveryTime).ToList(),
            "completed" => deliveries.Where(IsCompleted).OrderByDescending(d => d.DeliveryTime).ToList(),
            _ => deliveries.OrderByDescending(d => d.DeliveryTime).ToList()
        };

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

    private static string NormalizeTab(string? tab)
    {
        var normalized = (tab ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "all" => "all",
            "delivering" => "delivering",
            "completed" => "completed",
            _ => "all"
        };
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
