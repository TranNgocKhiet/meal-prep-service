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
public class CreateModel : PageModel
{
    private readonly IHealthProfileService _healthProfileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(IHealthProfileService healthProfileService, IUnitOfWork unitOfWork, ILogger<CreateModel> logger)
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

    public List<AllergyDto> AvailableAllergies { get; set; } = new();

    // Helper property for gender options
    public static List<string> GenderOptions => new List<string> { "Male", "Female", "Other" };

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            AccountId = GetCurrentAccountId();
            
            try
            {
                await _healthProfileService.GetByAccountIdAsync(AccountId);
                return RedirectToPage("/HealthProfile/Edit");
            }
            catch (BusinessException)
            {
                // Profile doesn't exist, continue with create
            }

            AvailableAllergies = (await _unitOfWork.Allergies.GetAllAsync())
                .Select(a => new AllergyDto { Id = a.Id, AllergyName = a.AllergyName })
                .ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create health profile page for account {AccountId}", GetCurrentAccountId());
            TempData["ErrorMessage"] = "An error occurred while loading the page.";
            return RedirectToPage("/Home/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
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

            var createdProfile = await _healthProfileService.CreateOrUpdateAsync(healthProfileDto);

            await AddSelectedAllergiesAsync(createdProfile.Id, SelectedAllergyIds);

            _logger.LogInformation("Health profile created for account {AccountId}", AccountId);
            TempData["SuccessMessage"] = "Health profile created successfully!";
            
            return RedirectToPage("/HealthProfile/Index");
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            AvailableAllergies = (await _unitOfWork.Allergies.GetAllAsync())
                .Select(a => new AllergyDto { Id = a.Id, AllergyName = a.AllergyName })
                .ToList();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating health profile for account {AccountId}", GetCurrentAccountId());
            ModelState.AddModelError(string.Empty, "An error occurred while creating your health profile.");
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

    private async Task AddSelectedAllergiesAsync(Guid profileId, List<Guid> selectedAllergyIds)
    {
        if (selectedAllergyIds?.Any() == true)
        {
            foreach (var allergyId in selectedAllergyIds)
            {
                try
                {
                    await _healthProfileService.AddAllergyAsync(profileId, allergyId);
                }
                catch (BusinessException ex)
                {
                    _logger.LogWarning("Failed to add allergy {AllergyId} to profile {ProfileId}: {Message}", 
                        allergyId, profileId, ex.Message);
                }
            }
        }
    }
}
