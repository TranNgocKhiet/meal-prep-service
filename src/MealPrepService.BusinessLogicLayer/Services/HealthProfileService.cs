using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using MealPrepService.DataAccessLayer.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class HealthProfileService : IHealthProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HealthProfileService> _logger;
        private readonly MealPrepDbContext _context;

        public HealthProfileService(
            IUnitOfWork unitOfWork,
            ILogger<HealthProfileService> logger,
            MealPrepDbContext context)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<HealthProfileDto> CreateOrUpdateAsync(HealthProfileDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            // Validate weight and height are positive
            if (dto.Weight <= 0)
            {
                throw new BusinessException("Weight must be a positive number");
            }

            if (dto.Height <= 0)
            {
                throw new BusinessException("Height must be a positive number");
            }

            // Validate age range
            if (dto.Age < 1 || dto.Age > 150)
            {
                throw new BusinessException("Age must be between 1 and 150");
            }

            if (string.IsNullOrWhiteSpace(dto.Gender))
            {
                throw new BusinessException("Gender is required");
            }

            // Check if account exists
            var account = await _unitOfWork.Accounts.GetByIdAsync(dto.AccountId);
            if (account == null)
            {
                throw new BusinessException($"Account with ID {dto.AccountId} not found");
            }

            // Check if profile already exists for this account
            var existingProfiles = await _unitOfWork.HealthProfiles.FindAsync(hp => hp.AccountId == dto.AccountId);
            var existingProfile = existingProfiles.FirstOrDefault();

            if (existingProfile != null)
            {
                // Update existing profile
                existingProfile.Age = dto.Age;
                existingProfile.Weight = dto.Weight;
                existingProfile.Height = dto.Height;
                existingProfile.Gender = dto.Gender;
                existingProfile.HealthNotes = dto.HealthNotes;
                existingProfile.DietaryRestrictions = dto.DietaryRestrictions;
                existingProfile.CalorieGoal = dto.CalorieGoal;
                existingProfile.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.HealthProfiles.UpdateAsync(existingProfile);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Health profile updated for account: {AccountId}", dto.AccountId);

                return await MapToDtoAsync(existingProfile);
            }
            else
            {
                // Create new profile
                var healthProfile = new HealthProfile
                {
                    Id = Guid.NewGuid(),
                    AccountId = dto.AccountId,
                    Age = dto.Age,
                    Weight = dto.Weight,
                    Height = dto.Height,
                    Gender = dto.Gender,
                    HealthNotes = dto.HealthNotes,
                    DietaryRestrictions = dto.DietaryRestrictions,
                    CalorieGoal = dto.CalorieGoal,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.HealthProfiles.AddAsync(healthProfile);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Health profile created for account: {AccountId}", dto.AccountId);

                return await MapToDtoAsync(healthProfile);
            }
        }

        public async Task<HealthProfileDto> GetByAccountIdAsync(Guid accountId)
        {
            var profiles = await _unitOfWork.HealthProfiles.FindAsync(hp => hp.AccountId == accountId);
            var profile = profiles.FirstOrDefault();

            if (profile == null)
            {
                throw new BusinessException($"Health profile not found for account {accountId}");
            }

            // Explicitly load navigation properties
            await _context.Entry(profile).Collection(p => p.Allergies).LoadAsync();
            await _context.Entry(profile).Collection(p => p.FoodPreferences).LoadAsync();

            return await MapToDtoAsync(profile);
        }

        public async Task AddAllergyAsync(Guid profileId, Guid allergyId)
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(profileId);
            if (profile == null)
            {
                throw new BusinessException($"Health profile with ID {profileId} not found");
            }

            var allergy = await _unitOfWork.Allergies.GetByIdAsync(allergyId);
            if (allergy == null)
            {
                throw new BusinessException($"Allergy with ID {allergyId} not found");
            }

            // Check if allergy is already linked
            if (profile.Allergies.Any(a => a.Id == allergyId))
            {
                _logger.LogWarning("Allergy {AllergyId} already linked to profile {ProfileId}", allergyId, profileId);
                return;
            }

            profile.Allergies.Add(allergy);
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.HealthProfiles.UpdateAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Allergy {AllergyId} added to profile {ProfileId}", allergyId, profileId);
        }

        public async Task RemoveAllergyAsync(Guid profileId, Guid allergyId)
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(profileId);
            if (profile == null)
            {
                throw new BusinessException($"Health profile with ID {profileId} not found");
            }

            var allergy = profile.Allergies.FirstOrDefault(a => a.Id == allergyId);
            if (allergy == null)
            {
                _logger.LogWarning("Allergy {AllergyId} not found in profile {ProfileId}", allergyId, profileId);
                return;
            }

            profile.Allergies.Remove(allergy);
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.HealthProfiles.UpdateAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Allergy {AllergyId} removed from profile {ProfileId}", allergyId, profileId);
        }

        public async Task AddFoodPreferenceAsync(Guid profileId, Guid preferenceId)
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(profileId);
            if (profile == null)
            {
                throw new BusinessException($"Health profile with ID {profileId} not found");
            }

            var preference = await _unitOfWork.FoodPreferences.GetByIdAsync(preferenceId);
            if (preference == null)
            {
                throw new BusinessException($"Food preference with ID {preferenceId} not found");
            }

            // Check if preference is already linked
            if (profile.FoodPreferences.Any(fp => fp.Id == preferenceId))
            {
                _logger.LogWarning("Food preference {PreferenceId} already linked to profile {ProfileId}", preferenceId, profileId);
                return;
            }

            profile.FoodPreferences.Add(preference);
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.HealthProfiles.UpdateAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Food preference {PreferenceId} added to profile {ProfileId}", preferenceId, profileId);
        }

        public async Task RemoveFoodPreferenceAsync(Guid profileId, Guid preferenceId)
        {
            var profile = await _unitOfWork.HealthProfiles.GetByIdAsync(profileId);
            if (profile == null)
            {
                throw new BusinessException($"Health profile with ID {profileId} not found");
            }

            var preference = profile.FoodPreferences.FirstOrDefault(fp => fp.Id == preferenceId);
            if (preference == null)
            {
                _logger.LogWarning("Food preference {PreferenceId} not found in profile {ProfileId}", preferenceId, profileId);
                return;
            }

            profile.FoodPreferences.Remove(preference);
            profile.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.HealthProfiles.UpdateAsync(profile);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Food preference {PreferenceId} removed from profile {ProfileId}", preferenceId, profileId);
        }

        private async Task<HealthProfileDto> MapToDtoAsync(HealthProfile profile)
        {
            return new HealthProfileDto
            {
                Id = profile.Id,
                AccountId = profile.AccountId,
                Age = profile.Age,
                Weight = profile.Weight,
                Height = profile.Height,
                Gender = profile.Gender,
                HealthNotes = profile.HealthNotes,
                DietaryRestrictions = profile.DietaryRestrictions,
                CalorieGoal = profile.CalorieGoal,
                Allergies = profile.Allergies.Select(a => a.AllergyName).ToList(),
                FoodPreferences = profile.FoodPreferences.Select(fp => fp.PreferenceName).ToList()
            };
        }
    }
}