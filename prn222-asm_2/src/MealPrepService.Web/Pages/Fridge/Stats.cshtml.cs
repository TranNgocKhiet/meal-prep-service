using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.Fridge;

[Authorize(Roles = "Customer")]
public class StatsModel : PageModel
{
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<StatsModel> _logger;

    public int TotalItems { get; set; }
    public int FreshItems { get; set; }
    public int ExpiringItems { get; set; }
    public int ExpiredItems { get; set; }

    public StatsModel(
        IFridgeService fridgeService,
        ILogger<StatsModel> logger)
    {
        _fridgeService = fridgeService ?? throw new ArgumentNullException(nameof(fridgeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
            var expiringItems = await _fridgeService.GetExpiringItemsAsync(accountId);
            
            TotalItems = fridgeItems.Count();
            FreshItems = fridgeItems.Count(item => !item.IsExpiring && !item.IsExpired);
            ExpiringItems = expiringItems.Count(item => item.IsExpiring && !item.IsExpired);
            ExpiredItems = expiringItems.Count(item => item.IsExpired);
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving fridge statistics for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading fridge statistics.";
            return RedirectToPage("/Fridge/Index");
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
