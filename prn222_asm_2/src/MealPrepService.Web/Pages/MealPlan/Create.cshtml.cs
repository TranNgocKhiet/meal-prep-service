using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class CreateModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ISystemConfigurationService _systemConfigService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IMealPlanService mealPlanService, ISystemConfigurationService systemConfigService, ILogger<CreateModel> logger)
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
    
    [BindProperty]
    public bool GenerateWithAI { get; set; }

    public int MaxMealPlanDays { get; set; }

    public async Task OnGetAsync()
    {
        MaxMealPlanDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
        EndDate = DateTime.Today.AddDays(MaxMealPlanDays);
        
        // Set ViewData property for the view
        ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        MaxMealPlanDays = await _systemConfigService.GetMaxMealPlanDaysAsync();

        if (!ModelState.IsValid)
        {
            ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
            return Page();
        }

        try
        {
            var accountId = GetCurrentAccountId();
            
            // Validate max days
            var daysDifference = (EndDate - StartDate).Days + 1;
            
            if (daysDifference > MaxMealPlanDays)
            {
                ModelState.AddModelError(string.Empty, $"Meal plan cannot exceed {MaxMealPlanDays} days. Current selection: {daysDifference} days.");
                ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
                return Page();
            }
            
            var mealPlanDto = new MealPlanDto
            {
                AccountId = accountId,
                PlanName = PlanName,
                StartDate = StartDate,
                EndDate = EndDate,
                IsAiGenerated = false
            };

            var createdPlan = await _mealPlanService.CreateManualMealPlanAsync(mealPlanDto);
            
            _logger.LogInformation("Manual meal plan {PlanName} created successfully for account {AccountId}", 
                PlanName, accountId);
            
            TempData["SuccessMessage"] = "Meal plan created successfully!";
            return RedirectToPage("/MealPlan/Details", new { id = createdPlan.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating meal plan for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while creating the meal plan. Please try again.");
            ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostGenerateAIAsync()
    {
        MaxMealPlanDays = await _systemConfigService.GetMaxMealPlanDaysAsync();

        if (!ModelState.IsValid)
        {
            ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
            return Page();
        }

        try
        {
            var accountId = GetCurrentAccountId();
            
            // Validate max days
            var daysDifference = (EndDate - StartDate).Days + 1;
            
            if (daysDifference > MaxMealPlanDays)
            {
                ModelState.AddModelError(string.Empty, $"Meal plan cannot exceed {MaxMealPlanDays} days. Current selection: {daysDifference} days.");
                ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
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
            ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating AI meal plan for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while generating the AI meal plan. Please try again.");
            ViewData["MaxMealPlanDays"] = MaxMealPlanDays;
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
