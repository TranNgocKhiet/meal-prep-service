using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    [Authorize(Roles = "Customer")]
    public class HealthProfileController : Controller
    {
        private readonly IHealthProfileService _healthProfileService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HealthProfileController> _logger;

        public HealthProfileController(
            IHealthProfileService healthProfileService,
            IUnitOfWork unitOfWork,
            ILogger<HealthProfileController> logger)
        {
            _healthProfileService = healthProfileService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var healthProfileDto = await _healthProfileService.GetByAccountIdAsync(accountId);
                
                var viewModel = await MapToViewModelAsync(healthProfileDto);
                return View(viewModel);
            }
            catch (BusinessException)
            {
                // Health profile doesn't exist, redirect to create
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health profile for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while retrieving your health profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                
                // Check if profile already exists
                try
                {
                    await _healthProfileService.GetByAccountIdAsync(accountId);
                    // Profile exists, redirect to edit
                    return RedirectToAction("Edit");
                }
                catch (BusinessException)
                {
                    // Profile doesn't exist, continue with create
                }

                var viewModel = new HealthProfileViewModel
                {
                    AccountId = accountId,
                    AvailableAllergies = await GetAvailableAllergiesAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create health profile page for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading the page.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HealthProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableAllergies = await GetAvailableAllergiesAsync();
                return View(model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                model.AccountId = accountId;

                var healthProfileDto = MapToDto(model);
                var createdProfile = await _healthProfileService.CreateOrUpdateAsync(healthProfileDto);

                // Add selected allergies
                await AddSelectedAllergiesAsync(createdProfile.Id, model.SelectedAllergyIds);

                _logger.LogInformation("Health profile created for account {AccountId}", accountId);
                TempData["SuccessMessage"] = "Health profile created successfully!";
                
                return RedirectToAction("Index");
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.AvailableAllergies = await GetAvailableAllergiesAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating health profile for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while creating your health profile.");
                model.AvailableAllergies = await GetAvailableAllergiesAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var healthProfileDto = await _healthProfileService.GetByAccountIdAsync(accountId);
                
                var viewModel = await MapToViewModelAsync(healthProfileDto);
                return View(viewModel);
            }
            catch (BusinessException)
            {
                // Health profile doesn't exist, redirect to create
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit health profile page for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading your health profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HealthProfileViewModel model)
        {
            _logger.LogInformation("Edit POST method called for account {AccountId} with Age: {Age}", GetCurrentAccountId(), model.Age);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for account {AccountId}. Errors: {Errors}", 
                    GetCurrentAccountId(), 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                model.AvailableAllergies = await GetAvailableAllergiesAsync();
                return View(model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                model.AccountId = accountId;

                var healthProfileDto = MapToDto(model);
                _logger.LogInformation("Updating health profile for account {AccountId}: Age={Age}, Weight={Weight}, Height={Height}", 
                    accountId, healthProfileDto.Age, healthProfileDto.Weight, healthProfileDto.Height);
                
                var updatedProfile = await _healthProfileService.CreateOrUpdateAsync(healthProfileDto);

                // Sync allergies based on checkboxes
                await SyncAllergiesAsync(updatedProfile.Id, model.SelectedAllergyIds);

                _logger.LogInformation("Health profile updated successfully for account {AccountId}", accountId);
                TempData["SuccessMessage"] = "Health profile updated successfully!";
                
                // Stay on the edit page after successful update
                return RedirectToAction("Edit");
            }
            catch (BusinessException ex)
            {
                _logger.LogError(ex, "Business exception updating health profile for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, ex.Message);
                model.AvailableAllergies = await GetAvailableAllergiesAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating health profile for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while updating your health profile.");
                model.AvailableAllergies = await GetAvailableAllergiesAsync();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAllergy(Guid allergyId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var healthProfileDto = await _healthProfileService.GetByAccountIdAsync(accountId);
                
                await _healthProfileService.AddAllergyAsync(healthProfileDto.Id, allergyId);
                
                _logger.LogInformation("Allergy {AllergyId} added to health profile for account {AccountId}", allergyId, accountId);
                TempData["SuccessMessage"] = "Allergy added successfully!";
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding allergy {AllergyId} for account {AccountId}", allergyId, GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while adding the allergy.";
            }

            // Check if we're on the edit page by looking at the referer
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("/Edit"))
            {
                return RedirectToAction("Edit");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAllergy(Guid allergyId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var healthProfileDto = await _healthProfileService.GetByAccountIdAsync(accountId);
                
                await _healthProfileService.RemoveAllergyAsync(healthProfileDto.Id, allergyId);
                
                _logger.LogInformation("Allergy {AllergyId} removed from health profile for account {AccountId}", allergyId, accountId);
                TempData["SuccessMessage"] = "Allergy removed successfully!";
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing allergy {AllergyId} for account {AccountId}", allergyId, GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while removing the allergy.";
            }

            // Check if we're on the edit page by looking at the referer
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("/Edit"))
            {
                return RedirectToAction("Edit");
            }
            return RedirectToAction("Index");
        }

        #region Helper Methods

        private Guid GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
            {
                throw new UnauthorizedAccessException("User is not properly authenticated");
            }
            return accountId;
        }

        private HealthProfileDto MapToDto(HealthProfileViewModel viewModel)
        {
            return new HealthProfileDto
            {
                Id = viewModel.Id,
                AccountId = viewModel.AccountId,
                Age = viewModel.Age,
                Weight = viewModel.Weight,
                Height = viewModel.Height,
                Gender = viewModel.Gender,
                HealthNotes = viewModel.HealthNotes,
                DietaryRestrictions = viewModel.DietaryRestrictions,
                FoodPreferences = viewModel.FoodPreferences,
                CalorieGoal = viewModel.CalorieGoal
            };
        }

        private async Task<HealthProfileViewModel> MapToViewModelAsync(HealthProfileDto dto)
        {
            var availableAllergies = await GetAvailableAllergiesAsync();

            return new HealthProfileViewModel
            {
                Id = dto.Id,
                AccountId = dto.AccountId,
                Age = dto.Age,
                Weight = dto.Weight,
                Height = dto.Height,
                Gender = dto.Gender,
                HealthNotes = dto.HealthNotes,
                DietaryRestrictions = dto.DietaryRestrictions,
                FoodPreferences = dto.FoodPreferences,
                CalorieGoal = dto.CalorieGoal,
                CurrentAllergies = dto.Allergies,
                AvailableAllergies = availableAllergies
            };
        }

        private async Task<List<AllergyViewModel>> GetAvailableAllergiesAsync()
        {
            var allergies = await _unitOfWork.Allergies.GetAllAsync();
            return allergies.Select(a => new AllergyViewModel
            {
                Id = a.Id,
                AllergyName = a.AllergyName,
                IsSelected = false
            }).ToList();
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

        private async Task SyncAllergiesAsync(Guid profileId, List<Guid> selectedAllergyIds)
        {
            // Get current profile with allergies
            var accountId = GetCurrentAccountId();
            var currentProfile = await _healthProfileService.GetByAccountIdAsync(accountId);
            
            // Get all available allergies to map names to IDs
            var allAllergies = await _unitOfWork.Allergies.GetAllAsync();
            var currentAllergyIds = allAllergies
                .Where(a => currentProfile.Allergies.Contains(a.AllergyName))
                .Select(a => a.Id)
                .ToList();

            var selectedIds = selectedAllergyIds ?? new List<Guid>();

            // Find allergies to remove (currently selected but not in the new selection)
            var allergiesToRemove = currentAllergyIds.Except(selectedIds).ToList();
            
            // Find allergies to add (in new selection but not currently selected)
            var allergiesToAdd = selectedIds.Except(currentAllergyIds).ToList();

            // Remove unchecked allergies
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

            // Add newly checked allergies
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

        #endregion
    }
}