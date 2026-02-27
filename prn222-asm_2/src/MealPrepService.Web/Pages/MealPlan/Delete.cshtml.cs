using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;


namespace MealPrepService.Web.Pages.MealPlan;

[Authorize(Roles = "Customer,Manager")]
public class DeleteModel : PageModel
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(IMealPlanService mealPlanService, ILogger<DeleteModel> logger)
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
                return Forbid("You don't have permission to delete this meal plan.");
            }

            MealPlan = mealPlanDto;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading delete confirmation for meal plan {MealPlanId}", id);
            TempData["ErrorMessage"] = "An error occurred while loading the meal plan.";
            return RedirectToPage("/MealPlan/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            await _mealPlanService.DeleteAsync(id, accountId);
            
            _logger.LogInformation("Meal plan {MealPlanId} deleted successfully by account {AccountId}", id, accountId);
            
            TempData["SuccessMessage"] = "Meal plan deleted successfully!";
            return RedirectToPage("/MealPlan/Index");
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/MealPlan/Index");
        }
        catch (AuthorizationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage("/MealPlan/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting meal plan {MealPlanId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the meal plan. Please try again.";
            return RedirectToPage("/MealPlan/Details", new { id });
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
