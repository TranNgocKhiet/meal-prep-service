using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

using MealPrepService.DataAccessLayer.Repositories;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class GenerateAIModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ISystemConfigurationService _systemConfigService;
    private readonly ILogger<GenerateAIModel> _logger;

    public GenerateAIModel(
        IMealPlanService mealPlanService,
        ISystemConfigurationService systemConfigService,
        ILogger<GenerateAIModel> logger)
    {
        _mealPlanService = mealPlanService;
        _systemConfigService = systemConfigService;
        _logger = logger;
    }

    [BindProperty]
    public string PlanName { get; set; } = string.Empty;

    [BindProperty]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [BindProperty]
    public DateTime EndDate { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
            ViewData["MaxMealPlanDays"] = maxDays;
            return Page();
        }

        try
        {
            var accountId = GetCurrentAccountId();
            
            // Validate max days
            var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
            var daysDifference = (EndDate - StartDate).Days + 1;
            
            if (daysDifference > maxDays)
            {
                ModelState.AddModelError(string.Empty, $"Meal plan cannot exceed {maxDays} days. Current selection: {daysDifference} days.");
                ViewData["MaxMealPlanDays"] = maxDays;
                return Page();
            }
            
            var aiGeneratedPlan = await _mealPlanService.GenerateAiMealPlanAsync(
                accountId, 
                StartDate, 
                EndDate,
                PlanName);
            
            _logger.LogInformation("AI meal plan '{PlanName}' generated successfully for account {AccountId}", 
                PlanName, accountId);
            
            TempData["SuccessMessage"] = "AI meal plan generated successfully!";
            return RedirectToPage("/MealPlan/Details", new { id = aiGeneratedPlan.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
            ViewData["MaxMealPlanDays"] = maxDays;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating AI meal plan for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while generating the AI meal plan. Please try again.");
            var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
            ViewData["MaxMealPlanDays"] = maxDays;
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
