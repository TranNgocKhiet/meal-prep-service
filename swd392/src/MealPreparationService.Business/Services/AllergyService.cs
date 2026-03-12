using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class AllergyService : IAllergyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AllergyService> _logger;

    public AllergyService(IUnitOfWork unitOfWork, ILogger<AllergyService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<AllergyDto>> GetAllAllergiesAsync()
    {
        _logger.LogInformation("Getting all allergies");
        
        try
        {
            var allergies = await _unitOfWork.Allergies.GetAllAsync();
            
            return allergies.Select(a => new AllergyDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = "",
                Severity = ""
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all allergies");
            throw;
        }
    }

    public async Task AddUserAllergyAsync(string userId, string allergyId)
    {
        _logger.LogInformation("Adding allergy {AllergyId} for user {UserId}", allergyId, userId);
        
        // TODO: Implement when needed
        await Task.CompletedTask;
        throw new NotImplementedException("AddUserAllergyAsync not yet implemented");
    }

    public async Task RemoveUserAllergyAsync(string userId, string allergyId)
    {
        _logger.LogInformation("Removing allergy {AllergyId} for user {UserId}", allergyId, userId);
        
        // TODO: Implement when needed
        await Task.CompletedTask;
        throw new NotImplementedException("RemoveUserAllergyAsync not yet implemented");
    }

    public async Task<List<AllergyDto>> GetUserAllergiesAsync(string userId)
    {
        _logger.LogInformation("Getting allergies for user {UserId}", userId);
        
        // TODO: Implement when needed
        await Task.CompletedTask;
        return new List<AllergyDto>();
    }

    public async Task<List<string>> CheckRecipeAllergensAsync(string recipeId, string userId)
    {
        _logger.LogInformation("Checking recipe allergens for recipe {RecipeId} and user {UserId}", recipeId, userId);
        
        // TODO: Implement when needed
        await Task.CompletedTask;
        return new List<string>();
    }

    public async Task<bool> HasAllergyWarningAsync(string recipeId, string userId)
    {
        _logger.LogInformation("Checking allergy warning for recipe {RecipeId} and user {UserId}", recipeId, userId);
        
        // TODO: Implement when needed
        await Task.CompletedTask;
        return false;
    }
}
