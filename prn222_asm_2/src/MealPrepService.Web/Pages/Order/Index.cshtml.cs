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

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var orderDtos = await _orderService.GetByAccountIdAsync(accountId);
            
            Orders = orderDtos
                .OrderByDescending(o => o.OrderDate)
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
