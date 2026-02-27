using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;

using MealPrepService.DataAccessLayer.Repositories;
using System.Security.Claims;

namespace MealPrepService.Web.Pages.HealthProfile;

[Authorize(Roles = "Customer")]
public class IndexModel : PageModel
{
    private readonly IHealthProfileService _healthProfileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHealthProfileService healthProfileService, IUnitOfWork unitOfWork, ILogger<IndexModel> logger)
    {
        _healthProfileService = healthProfileService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public HealthProfileDto Profile { get; set; } = new();

    // Helper properties for view binding
    public int Age => Profile.Age;
    public string Gender => Profile.Gender;
    public float Weight => Profile.Weight;
    public float Height => Profile.Height;
    public int? CalorieGoal => Profile.CalorieGoal;
    public string? HealthNotes => Profile.HealthNotes;
    public string? DietaryRestrictions => Profile.DietaryRestrictions;
    public string? FoodPreferences => Profile.FoodPreferences;
    public List<string> CurrentAllergies => Profile.Allergies;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var accountId = GetCurrentAccountId();
            Profile = await _healthProfileService.GetByAccountIdAsync(accountId);
            
            return Page();
        }
        catch (BusinessException)
        {
            return RedirectToPage("/HealthProfile/Create");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health profile for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while retrieving your health profile.";
            return RedirectToPage("/Home/Index");
        }
    }

    private Guid GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new UnauthorizedAccessException("User is not properly authenticated");
        }
        return accountId;
    }
}
