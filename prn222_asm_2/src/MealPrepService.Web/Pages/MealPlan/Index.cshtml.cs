using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class IndexModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IMealPlanService mealPlanService, ILogger<IndexModel> logger)
    {
        _mealPlanService = mealPlanService;
        _logger = logger;
    }

    public List<MealPlanDto> MealPlans { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            var mealPlanDtos = await _mealPlanService.GetByAccountIdAsync(accountId);
            
            // Sort meal plans: Active plans first, then by start date descending
            MealPlans = mealPlanDtos
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.StartDate)
                .ToList();
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving meal plans for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading your meal plans.";
            MealPlans = new List<MealPlanDto>();
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
