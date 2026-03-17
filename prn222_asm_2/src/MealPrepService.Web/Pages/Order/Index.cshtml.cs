using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IOrderService orderService, ILogger<IndexModel> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public List<OrderDto> Orders { get; set; } = new();
    public List<OrderDto> FilteredOrders { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<string> AvailableStatuses { get; private set; } = Array.Empty<string>();
    public int PageSize { get; } = 6;
    public int TotalOrders { get; private set; }
    public int TotalPages { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var orderDtos = await _orderService.GetByAccountIdAsync(accountId);

            AvailableStatuses = orderDtos
                .Select(o => o.Status)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            if (!string.Equals(StatusFilter, "all", StringComparison.OrdinalIgnoreCase))
            {
                orderDtos = orderDtos
                    .Where(o => string.Equals(o.Status, StatusFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            FilteredOrders = orderDtos
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            TotalOrders = FilteredOrders.Count;
            TotalPages = TotalOrders == 0
                ? 1
                : (int)Math.Ceiling(TotalOrders / (double)PageSize);

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            Orders = FilteredOrders
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving orders for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading your orders.";
            Orders = new List<OrderDto>();
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
