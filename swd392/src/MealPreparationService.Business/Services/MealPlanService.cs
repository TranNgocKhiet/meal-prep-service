using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.Business.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MealPlanService> _logger;

    public MealPlanService(IUnitOfWork unitOfWork, ILogger<MealPlanService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MealPlanDto> CreateCustomMealPlanAsync(CreateMealPlanDto dto, string userId)
    {
        _logger.LogInformation("Creating custom meal plan for user {UserId}", userId);
        
        try
        {
            // Get or create health profile for the user
            var healthProfile = await _unitOfWork.HealthProfiles.GetByAccountIdAsync(userId);
            
            if (healthProfile == null)
            {
                _logger.LogInformation("Creating new health profile for user {UserId}", userId);
                healthProfile = new HealthProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    AccountId = userId,
                    Age = 0,
                    Weight = 0,
                    Height = 0,
                    Gender = "",
                    HealthNotes = "",
                    CalorieGoal = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                healthProfile = await _unitOfWork.HealthProfiles.AddAsync(healthProfile);
            }
            
            // Process allergies
            if (dto.Allergies != null && dto.Allergies.Any())
            {
                _logger.LogInformation("Processing {Count} allergies for health profile {HealthProfileId}", 
                    dto.Allergies.Count, healthProfile.Id);
                
                foreach (var allergyId in dto.Allergies)
                {
                    var exists = await _unitOfWork.HealthProfileAllergies.ExistsAsync(healthProfile.Id, allergyId);
                    if (!exists)
                    {
                        var healthProfileAllergy = new HealthProfileAllergy
                        {
                            HealthProfileId = healthProfile.Id,
                            AllergyId = allergyId
                        };
                        await _unitOfWork.HealthProfileAllergies.AddAsync(healthProfileAllergy);
                    }
                }
            }
            
            // Process liked ingredients
            if (dto.LikedIngredients != null && dto.LikedIngredients.Any())
            {
                _logger.LogInformation("Processing {Count} liked ingredients for health profile {HealthProfileId}", 
                    dto.LikedIngredients.Count, healthProfile.Id);
                
                foreach (var ingredientId in dto.LikedIngredients)
                {
                    var exists = await _unitOfWork.HealthProfileIngredients.ExistsAsync(healthProfile.Id, ingredientId);
                    if (!exists)
                    {
                        var healthProfileIngredient = new HealthProfileIngredient
                        {
                            HealthProfileId = healthProfile.Id,
                            IngredientId = ingredientId,
                            RelationshipTypeId = 1 // 1 = Like
                        };
                        await _unitOfWork.HealthProfileIngredients.AddAsync(healthProfileIngredient);
                    }
                }
            }
            
            // Process disliked ingredients
            if (dto.DislikedIngredients != null && dto.DislikedIngredients.Any())
            {
                _logger.LogInformation("Processing {Count} disliked ingredients for health profile {HealthProfileId}", 
                    dto.DislikedIngredients.Count, healthProfile.Id);
                
                foreach (var ingredientId in dto.DislikedIngredients)
                {
                    var exists = await _unitOfWork.HealthProfileIngredients.ExistsAsync(healthProfile.Id, ingredientId);
                    if (!exists)
                    {
                        var healthProfileIngredient = new HealthProfileIngredient
                        {
                            HealthProfileId = healthProfile.Id,
                            IngredientId = ingredientId,
                            RelationshipTypeId = 2 // 2 = Dislike
                        };
                        await _unitOfWork.HealthProfileIngredients.AddAsync(healthProfileIngredient);
                    }
                }
            }
            
            // Process allergy ingredients
            if (dto.AllergyIngredients != null && dto.AllergyIngredients.Any())
            {
                _logger.LogInformation("Processing {Count} allergy ingredients for health profile {HealthProfileId}", 
                    dto.AllergyIngredients.Count, healthProfile.Id);
                
                foreach (var ingredientId in dto.AllergyIngredients)
                {
                    var exists = await _unitOfWork.HealthProfileIngredients.ExistsAsync(healthProfile.Id, ingredientId);
                    if (!exists)
                    {
                        var healthProfileIngredient = new HealthProfileIngredient
                        {
                            HealthProfileId = healthProfile.Id,
                            IngredientId = ingredientId,
                            RelationshipTypeId = 3 // 3 = Allergen
                        };
                        await _unitOfWork.HealthProfileIngredients.AddAsync(healthProfileIngredient);
                    }
                }
            }
            
            // Apply defaults for null values
            var durationDays = dto.DurationDays ?? 1;
            var startDate = dto.StartDate ?? DateTime.Today;
            
            // Create the meal plan
            var mealPlan = new MealPlan
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = userId,
                PlanName = dto.Name,
                StartDate = startDate,
                EndDate = startDate.AddDays(durationDays - 1),
                IsAiGenerated = false,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // Personal Information
                Age = dto.Age,
                Weight = dto.Weight,
                Height = dto.Height,
                Gender = dto.Gender,
                HealthNote = dto.HealthNote,
                CaloriesGoal = dto.CaloriesGoal
            };
            
            mealPlan = await _unitOfWork.MealPlans.AddAsync(mealPlan);
            
            // Create meals for each day
            var meals = new List<Meal>();
            for (int day = 0; day < durationDays; day++)
            {
                var currentDate = startDate.AddDays(day);
                
                // Create 3 meals per day (Breakfast, Lunch, Dinner)
                for (int mealType = 1; mealType <= 3; mealType++)
                {
                    var meal = new Meal
                    {
                        Id = Guid.NewGuid().ToString(),
                        PlanId = mealPlan.Id,
                        MealTypeId = mealType,
                        ServerDate = currentDate,
                        TotalCalories = 0,
                        ProteinG = 0,
                        FatG = 0,
                        CarbsG = 0,
                        MealFinished = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    meals.Add(meal);
                }
            }
            
            // Add all meals to the meal plan
            foreach (var meal in meals)
            {
                mealPlan.Meals.Add(meal);
            }
            
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Successfully created meal plan {MealPlanId} with {MealCount} meals for user {UserId}", 
                mealPlan.Id, meals.Count, userId);
            
            // Return the full meal plan with days
            return await GetMealPlanByIdAsync(mealPlan.Id, userId) 
                ?? throw new InvalidOperationException("Failed to retrieve created meal plan");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom meal plan for user {UserId}", userId);
            throw;
        }
    }

    public async Task<MealPlanDto> CreateAiGeneratedMealPlanAsync(AiMealPlanRequestDto dto, string userId)
    {
        _logger.LogInformation("Creating AI-generated meal plan for user {UserId}", userId);
        
        // TODO: Implement AI meal plan generation logic
        await Task.CompletedTask;
        
        return new MealPlanDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"AI Meal Plan - {DateTime.Now:yyyy-MM-dd}",
            DurationDays = 7,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            IsAiGenerated = true,
            Status = "Active",
            Days = new List<MealPlanDayDto>(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<MealPlanDto> UpdateMealPlanAsync(string mealPlanId, UpdateMealPlanDto dto, string userId)
    {
        _logger.LogInformation("Updating meal plan {MealPlanId} for user {UserId}", mealPlanId, userId);
        
        // Find the meal plan with related entities
        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
            .FirstOrDefaultAsync(mp => mp.Id == mealPlanId && mp.AccountId == userId);

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            mealPlan.PlanName = dto.Name;
        }

        // Update meals if provided
        if (dto.Days != null && dto.Days.Any())
        {
            foreach (var dayDto in dto.Days)
            {
                foreach (var mealDto in dayDto.Meals)
                {
                    // Find the meal by ID if provided, otherwise by day number and meal type
                    Meal? meal = null;
                    
                    if (!string.IsNullOrEmpty(mealDto.Id))
                    {
                        meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealDto.Id);
                    }
                    else
                    {
                        // Calculate the date for this day number
                        var targetDate = mealPlan.StartDate.Date.AddDays(dayDto.DayNumber - 1);
                        meal = mealPlan.Meals.FirstOrDefault(m => 
                            m.ServerDate.Date == targetDate && 
                            m.MealTypeId == mealDto.MealTypeId);
                    }
                    
                    if (meal != null)
                    {
                        // Remove existing recipes
                        var existingRecipes = meal.MealRecipes.ToList();
                        foreach (var mr in existingRecipes)
                        {
                            meal.MealRecipes.Remove(mr);
                        }

                        // Add new recipes
                        foreach (var recipeId in mealDto.RecipeIds)
                        {
                            meal.MealRecipes.Add(new MealRecipe
                            {
                                Id = Guid.NewGuid().ToString(),
                                MealId = meal.Id,
                                RecipeId = recipeId
                            });
                        }

                        // Recalculate nutrition
                        await RecalculateMealNutrition(meal);
                    }
                }
            }
        }

        mealPlan.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        // Return updated meal plan
        return await GetMealPlanByIdAsync(mealPlanId, userId) 
            ?? throw new KeyNotFoundException($"Meal plan {mealPlanId} not found after update");
    }

    private async Task RecalculateMealNutrition(Meal meal)
    {
        if (!meal.MealRecipes.Any())
        {
            meal.TotalCalories = 0;
            meal.ProteinG = 0;
            meal.FatG = 0;
            meal.CarbsG = 0;
            return;
        }

        var recipeIds = meal.MealRecipes.Select(mr => mr.RecipeId).ToList();
        var recipes = await _unitOfWork.Recipes.GetAllQueryable()
            .Where(r => recipeIds.Contains(r.Id))
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                    .ThenInclude(i => i.IngredientNutrients)
                        .ThenInclude(inu => inu.Nutrient)
            .ToListAsync();

        decimal totalCalories = 0;
        decimal totalProtein = 0;
        decimal totalFat = 0;
        decimal totalCarbs = 0;

        foreach (var recipe in recipes)
        {
            foreach (var recipeIngredient in recipe.RecipeIngredients)
            {
                var ingredient = recipeIngredient.Ingredient;
                var amount = recipeIngredient.Amount;

                foreach (var nutrient in ingredient.IngredientNutrients)
                {
                    var nutrientName = nutrient.Nutrient.Name.ToLower().Trim();
                    var value = nutrient.AmountPer100 * amount / 100;

                    // Check each nutrient type independently (not else-if)
                    if (nutrientName.Contains("protein"))
                    {
                        totalProtein += value;
                    }
                    if (nutrientName.Contains("fat") && !nutrientName.Contains("saturated"))
                    {
                        totalFat += value;
                    }
                    if (nutrientName.Contains("carb"))
                    {
                        totalCarbs += value;
                    }
                    if (nutrientName.Contains("calor") || nutrientName.Contains("energy"))
                    {
                        totalCalories += value;
                    }
                }
            }
        }

        meal.TotalCalories = totalCalories;
        meal.ProteinG = totalProtein;
        meal.FatG = totalFat;
        meal.CarbsG = totalCarbs;
    }

    public async Task DeleteMealPlanAsync(string mealPlanId, string userId)
    {
        _logger.LogInformation("Deleting meal plan {MealPlanId} for user {UserId}", mealPlanId, userId);

        // First verify the meal plan exists and belongs to the user
        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .FirstOrDefaultAsync(mp => mp.Id == mealPlanId && mp.AccountId == userId);

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        // Delete the meal plan - cascade delete should handle related entities
        await _unitOfWork.MealPlans.DeleteAsync(mealPlanId);

        _logger.LogInformation("Successfully deleted meal plan {MealPlanId}", mealPlanId);
    }


    public async Task<MealPlanDto?> GetMealPlanByIdAsync(string mealPlanId, string userId)
    {
        _logger.LogInformation("Getting meal plan {MealPlanId} for user {UserId}", mealPlanId, userId);
        
        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .Where(mp => mp.Id == mealPlanId && mp.AccountId == userId)
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
                    .ThenInclude(mr => mr.Recipe)
                        .ThenInclude(r => r.RecipeIngredients)
                            .ThenInclude(ri => ri.Ingredient)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (mealPlan == null)
        {
            return null;
        }

        var dto = new MealPlanDto
        {
            Id = mealPlan.Id,
            Name = mealPlan.PlanName,
            DurationDays = (mealPlan.EndDate - mealPlan.StartDate).Days + 1,
            StartDate = mealPlan.StartDate,
            EndDate = mealPlan.EndDate,
            IsAiGenerated = mealPlan.IsAiGenerated,
            IsActive = mealPlan.IsActive,
            Status = GetMealPlanStatus(mealPlan),
            CreatedAt = mealPlan.CreatedAt,
            Days = BuildMealPlanDays(mealPlan),
            // Personal Information
            Age = mealPlan.Age,
            Weight = mealPlan.Weight,
            Height = mealPlan.Height,
            Gender = mealPlan.Gender,
            HealthNote = mealPlan.HealthNote,
            CaloriesGoal = mealPlan.CaloriesGoal
        };

        return dto;
    }

    public async Task<List<MealPlanDto>> GetUserMealPlansAsync(string userId)
    {
        _logger.LogInformation("Getting meal plans for user {UserId}", userId);
        
        var mealPlans = await _unitOfWork.MealPlans.GetAllQueryable()
            .Where(mp => mp.AccountId == userId)
            .Include(mp => mp.Meals)
                .ThenInclude(m => m.MealRecipes)
                    .ThenInclude(mr => mr.Recipe)
            .OrderByDescending(mp => mp.CreatedAt)
            .ToListAsync();

        var mealPlanDtos = new List<MealPlanDto>();
        
        foreach (var mealPlan in mealPlans)
        {
            var dto = new MealPlanDto
            {
                Id = mealPlan.Id,
                Name = mealPlan.PlanName,
                DurationDays = (mealPlan.EndDate - mealPlan.StartDate).Days + 1,
                StartDate = mealPlan.StartDate,
                EndDate = mealPlan.EndDate,
                IsAiGenerated = mealPlan.IsAiGenerated,
                IsActive = mealPlan.IsActive,
                Status = GetMealPlanStatus(mealPlan),
                CreatedAt = mealPlan.CreatedAt,
                Days = BuildMealPlanDays(mealPlan)
            };
            
            mealPlanDtos.Add(dto);
        }
        
        return mealPlanDtos;
    }

    private string GetMealPlanStatus(MealPlan mealPlan)
    {
        var now = DateTime.UtcNow.Date;
        
        if (now < mealPlan.StartDate.Date)
            return "Pending";
        else if (now > mealPlan.EndDate.Date)
            return "Completed";
        else if (mealPlan.IsActive)
            return "Active";
        else
            return "Inactive";
    }

    private List<MealPlanDayDto> BuildMealPlanDays(MealPlan mealPlan)
    {
        var days = new List<MealPlanDayDto>();
        var currentDate = mealPlan.StartDate.Date;
        var dayNumber = 1;

        while (currentDate <= mealPlan.EndDate.Date)
        {
            var dayMeals = mealPlan.Meals
                .Where(m => m.ServerDate.Date == currentDate)
                .OrderBy(m => m.MealTypeId)
                .ToList();

            var mealDtos = new List<MealDto>();
            foreach (var meal in dayMeals)
            {
                var recipes = meal.MealRecipes
                    .Select(mr => new RecipeDto
                    {
                        Id = mr.Recipe.Id,
                        RecipeName = mr.Recipe.RecipeName,
                        Instructions = mr.Recipe.Instructions
                    })
                    .ToList();

                mealDtos.Add(new MealDto
                {
                    Id = meal.Id,
                    MealTypeId = meal.MealTypeId,
                    RecipeIds = meal.MealRecipes.Select(mr => mr.RecipeId).ToList(),
                    Recipes = recipes,
                    Status = meal.MealFinished ? "Finished" : "Pending",
                    Date = meal.ServerDate,
                    MealPlanId = mealPlan.Id,
                    TotalCalories = meal.TotalCalories,
                    ProteinG = meal.ProteinG,
                    FatG = meal.FatG,
                    CarbsG = meal.CarbsG
                });
            }

            days.Add(new MealPlanDayDto
            {
                DayNumber = dayNumber,
                Date = currentDate,
                Meals = mealDtos
            });

            currentDate = currentDate.AddDays(1);
            dayNumber++;
        }

        return days;
    }

    public async Task<bool> ValidateMealPlanLimitsAsync(string userId)
    {
        _logger.LogInformation("Validating meal plan limits for user {UserId}", userId);
        
        // TODO: Implement actual validation logic
        await Task.CompletedTask;
        
        // Allow meal plan creation for now
        return true;
    }

    public async Task<MealPlanDto> SetActiveMealPlanAsync(string mealPlanId, string userId)
    {
        _logger.LogInformation("Toggling active status for meal plan {MealPlanId} for user {UserId}", mealPlanId, userId);
        
        // Find the meal plan
        var mealPlan = await _unitOfWork.MealPlans.GetAllQueryable()
            .FirstOrDefaultAsync(mp => mp.Id == mealPlanId && mp.AccountId == userId);

        if (mealPlan == null)
        {
            throw new KeyNotFoundException($"Meal plan {mealPlanId} not found");
        }

        // Toggle the active status
        mealPlan.IsActive = !mealPlan.IsActive;
        mealPlan.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully toggled meal plan {MealPlanId} active status to {IsActive} for user {UserId}", 
            mealPlanId, mealPlan.IsActive, userId);

        // Return the updated meal plan
        return await GetMealPlanByIdAsync(mealPlanId, userId) 
            ?? throw new KeyNotFoundException($"Meal plan {mealPlanId} not found after update");
    }
}
