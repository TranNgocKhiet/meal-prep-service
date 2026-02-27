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
public class EditModel : PageModel
{
    private readonly IHealthProfileService _healthProfileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IHealthProfileService healthProfileService, IUnitOfWork unitOfWork, ILogger<EditModel> logger)
    {
        _healthProfileService = healthProfileService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public Guid AccountId { get; set; }

    [BindProperty]
    public int Age { get; set; }

    [BindProperty]
    public float Weight { get; set; }

    [BindProperty]
    public float Height { get; set; }

    [BindProperty]
    public string Gender { get; set; } = string.Empty;

    [BindProperty]
    public string? HealthNotes { get; set; }

    [BindProperty]
    public string? DietaryRestrictions { get; set; }

    [BindProperty]
    public string? FoodPreferences { get; set; }

    [BindProperty]
    public int? CalorieGoal { get; set; }

    [BindProperty]
    public List<Guid> SelectedAllergyIds { get; set; } = new();

    public List<string> CurrentAllergies { get; set; } = new();
    public List<AllergyDto> AvailableAllergies { get; set; } = new();

    // Helper property for gender options
    public static List<string> GenderOptions => new List<string> { "Male", "Female", "Other" };

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            AccountId = GetCurrentAccountId();
            var healthProfileDto = await _healthProfileService.GetByAccountIdAsync(AccountId);
            
            Id = healthProfileDto.Id;
            Age = healthProfileDto.Age;
            Weight = healthProfileDto.Weight;
            Height = healthProfileDto.Height;
            Gender = healthProfileDto.Gender;
            HealthNotes = healthProfileDto.HealthNotes;
            DietaryRestrictions = healthProfileDto.DietaryRestrictions;
            FoodPreferences = healthProfileDto.FoodPreferences;
            CalorieGoal = healthProfileDto.CalorieGoal;
            CurrentAllergies = healthProfileDto.Allergies;
            AvailableAllergies = (await _unitOfWork.Allergies.GetAllAsync())
                .Select(a => new AllergyDto { Id = a.Id, AllergyName = a.AllergyName })
                .ToList();

            return Page();
        }
        catch (BusinessException)
        {
            return RedirectToPage("/HealthProfile/Create");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading edit health profile page for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading your health profile.";
            return RedirectToPage("/Home/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("Edit POST method called for account {AccountId} with Age: {Age}", GetCurrentAccountId(), Age);
        
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state is invalid for account {AccountId}. Errors: {Errors}", 
                GetCurrentAccountId(), 
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            
            AvailableAllergies = (await _unitOfWork.Allergies.GetAllAsync())
                .Select(a => new AllergyDto { Id = a.Id, AllergyName = a.AllergyName })
                .ToList();
            return Page();
        }

        try
        {
            AccountId = GetCurrentAccountId();

            var healthProfileDto = new HealthProfileDto
            {
                Id = Id,
                AccountId = AccountId,
                Age = Age,
                Weight = Weight,
                Height = Height,
                Gender = Gender,
                HealthNotes = HealthNotes,
                DietaryRestrictions = DietaryRestrictions,
                FoodPreferences = FoodPreferences,
                CalorieGoal = CalorieGoal
            };

            _logger.LogInformation("Updating health profile for account {AccountId}: Age={Age}, Weight={Weight}, Height={Height}", 
                AccountId, healthProfileDto.Age, healthProfileDto.Weight, healthProfileDto.Height);
            
            var updatedProfile = await _healthProfileService.CreateOrUpdateAsync(healthProfileDto);

            await SyncAllergiesAsync(updatedProfile.Id, SelectedAllergyIds);

            _logger.LogInformation("Health profile updated successfully for account {AccountId}", AccountId);
            TempData["SuccessMessage"] = "Health profile updated successfully!";
            
            return RedirectToPage("/HealthProfile/Edit");
        }
        catch (BusinessException ex)
        {
            _logger.LogError(ex, "Business exception updating health profile for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableAllergies = (await _unitOfWork.Allergies.GetAllAsync())
                .Select(a => new AllergyDto { Id = a.Id, AllergyName = a.AllergyName })
                .ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health profile for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while updating your health profile.");
            AvailableAllergies = (await _unitOfWork.Allergies.GetAllAsync())
                .Select(a => new AllergyDto { Id = a.Id, AllergyName = a.AllergyName })
                .ToList();
            return Page();
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

    private async Task SyncAllergiesAsync(Guid profileId, List<Guid> selectedAllergyIds)
    {
        var accountId = GetCurrentAccountId();
        var currentProfile = await _healthProfileService.GetByAccountIdAsync(accountId);
        
        var allAllergies = await _unitOfWork.Allergies.GetAllAsync();
        var currentAllergyIds = allAllergies
            .Where(a => currentProfile.Allergies.Contains(a.AllergyName))
            .Select(a => a.Id)
            .ToList();

        var selectedIds = selectedAllergyIds ?? new List<Guid>();

        var allergiesToRemove = currentAllergyIds.Except(selectedIds).ToList();
        var allergiesToAdd = selectedIds.Except(currentAllergyIds).ToList();

        foreach (var allergyId in allergiesToRemove)
        {
            try
            {
                await _healthProfileService.RemoveAllergyAsync(profileId, allergyId);
                _logger.LogInformation("Removed allergy {AllergyId} from profile {ProfileId}", allergyId, profileId);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning("Failed to remove allergy {AllergyId} from profile {ProfileId}: {Message}", 
                    allergyId, profileId, ex.Message);
            }
        }

        foreach (var allergyId in allergiesToAdd)
        {
            try
            {
                await _healthProfileService.AddAllergyAsync(profileId, allergyId);
                _logger.LogInformation("Added allergy {AllergyId} to profile {ProfileId}", allergyId, profileId);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning("Failed to add allergy {AllergyId} to profile {ProfileId}: {Message}", 
                    allergyId, profileId, ex.Message);
            }
        }
    }
}
