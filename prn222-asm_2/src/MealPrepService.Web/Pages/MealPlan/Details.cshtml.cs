using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class DetailsModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IMealPlanService mealPlanService, ILogger<DetailsModel> logger)
    {
        _mealPlanService = mealPlanService;
        _logger = logger;
    }

    public MealPlanDto MealPlan { get; set; } = new();

    // Helper properties for view
    public Guid Id => MealPlan?.Id ?? Guid.Empty;
    public string PlanName => MealPlan?.PlanName ?? string.Empty;
    public DateTime StartDate => MealPlan?.StartDate ?? DateTime.MinValue;
    public DateTime EndDate => MealPlan?.EndDate ?? DateTime.MinValue;
    public bool IsAiGenerated => MealPlan?.IsAiGenerated ?? false;
    public List<MealDto> Meals => MealPlan?.Meals ?? new List<MealDto>();
    public decimal TotalCalories => MealPlan?.TotalCalories ?? 0;
    public decimal TotalProteinG => MealPlan?.TotalProteinG ?? 0;
    public decimal TotalFatG => MealPlan?.TotalFatG ?? 0;
    public decimal TotalCarbsG => MealPlan?.TotalCarbsG ?? 0;
    public decimal FinishedCalories => MealPlan?.FinishedCalories ?? 0;
    public decimal FinishedProteinG => MealPlan?.FinishedProteinG ?? 0;
    public decimal FinishedFatG => MealPlan?.FinishedFatG ?? 0;
    public decimal FinishedCarbsG => MealPlan?.FinishedCarbsG ?? 0;
    public Dictionary<DateTime, DailyMealsWrapper> DailyNutrition => MealPlan?.Meals?
        .GroupBy(m => m.ServeDate.Date)
        .ToDictionary(g => g.Key, g => new DailyMealsWrapper { Meals = g.ToList() }) 
        ?? new Dictionary<DateTime, DailyMealsWrapper>();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var mealPlanDto = await _mealPlanService.GetByIdAsync(id);
            
            if (mealPlanDto == null)
            {
                return NotFound("Meal plan not found.");
            }

            var accountId = GetCurrentAccountId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (mealPlanDto.AccountId != accountId && userRole != "Manager")
            {
                return Forbid("You don't have permission to view this meal plan.");
            }

            MealPlan = mealPlanDto;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving meal plan {MealPlanId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the meal plan.";
            return RedirectToPage("/MealPlan/Index");
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

    // Helper class for daily nutrition display
    public class DailyMealsWrapper
    {
        public List<MealDto> Meals { get; set; } = new();
        public decimal DailyCalories => Meals?.Sum(m => m.TotalCalories) ?? 0;
        public decimal DailyProteinG => Meals?.Sum(m => m.TotalProteinG) ?? 0;
        public decimal DailyFatG => Meals?.Sum(m => m.TotalFatG) ?? 0;
        public decimal DailyCarbsG => Meals?.Sum(m => m.TotalCarbsG) ?? 0;
    }

    // Helper method for meal type ordering
    public static int GetMealTypeOrder(string mealType)
    {
        return mealType?.ToLower() switch
        {
            "breakfast" => 1,
            "lunch" => 2,
            "dinner" => 3,
            "snack" => 4,
            _ => 5
        };
    }
}
