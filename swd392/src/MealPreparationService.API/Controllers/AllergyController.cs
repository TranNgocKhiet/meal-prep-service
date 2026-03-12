using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/allergies")]
[Authorize]
public class AllergyController : ControllerBase
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<AllergyController> _logger;

    public AllergyController(IAllergyService allergyService, ILogger<AllergyController> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    [HttpGet("all")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<List<AllergyDto>>>> GetAllAllergies()
    {
        try
        {
            var allergies = await _allergyService.GetAllAllergiesAsync();
            return Ok(ApiResponse<List<AllergyDto>>.SuccessResponse(allergies, "Allergies retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all allergies");
            return StatusCode(500, ApiResponse<List<AllergyDto>>.ErrorResponse("An error occurred while retrieving allergies"));
        }
    }

    [HttpGet("user")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<List<AllergyDto>>>> GetUserAllergies()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<List<AllergyDto>>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var allergies = await _allergyService.GetUserAllergiesAsync(userId);
            return Ok(ApiResponse<List<AllergyDto>>.SuccessResponse(allergies, "User allergies retrieved successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid user ID: {UserId}", userId);
            return BadRequest(ApiResponse<List<AllergyDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user allergies for user {UserId}", userId);
            return StatusCode(500, ApiResponse<List<AllergyDto>>.ErrorResponse("An error occurred while retrieving user allergies"));
        }
    }

    [HttpPost("user/{allergyId}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<object>>> AddUserAllergy(string allergyId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        try
        {
            await _allergyService.AddUserAllergyAsync(userId, allergyId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Allergy added successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for adding allergy: UserId={UserId}, AllergyId={AllergyId}", userId, allergyId);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User {UserId} already has allergy {AllergyId}", userId, allergyId);
            return Conflict(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding allergy {AllergyId} for user {UserId}", allergyId, userId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while adding the allergy"));
        }
    }

    [HttpDelete("user/{allergyId}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveUserAllergy(string allergyId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        try
        {
            await _allergyService.RemoveUserAllergyAsync(userId, allergyId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Allergy removed successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for removing allergy: UserId={UserId}, AllergyId={AllergyId}", userId, allergyId);
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User {UserId} does not have allergy {AllergyId}", userId, allergyId);
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing allergy {AllergyId} for user {UserId}", allergyId, userId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while removing the allergy"));
        }
    }

    [HttpGet("recipe/{recipeId}/check")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<RecipeAllergenCheckResult>>> CheckRecipeAllergens(string recipeId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<RecipeAllergenCheckResult>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var allergens = await _allergyService.CheckRecipeAllergensAsync(recipeId, userId);
            var result = new RecipeAllergenCheckResult
            {
                HasWarning = allergens.Any(),
                Allergens = allergens
            };

            return Ok(ApiResponse<RecipeAllergenCheckResult>.SuccessResponse(result, "Recipe allergens checked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking recipe allergens for recipe {RecipeId} and user {UserId}", recipeId, userId);
            return StatusCode(500, ApiResponse<RecipeAllergenCheckResult>.ErrorResponse("An error occurred while checking recipe allergens"));
        }
    }

    [HttpGet("recipe/{recipeId}/warning")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<AllergyWarningResult>>> HasAllergyWarning(string recipeId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<AllergyWarningResult>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var hasWarning = await _allergyService.HasAllergyWarningAsync(recipeId, userId);
            var result = new AllergyWarningResult { HasWarning = hasWarning };

            return Ok(ApiResponse<AllergyWarningResult>.SuccessResponse(result, "Allergy warning checked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking allergy warning for recipe {RecipeId} and user {UserId}", recipeId, userId);
            return StatusCode(500, ApiResponse<AllergyWarningResult>.ErrorResponse("An error occurred while checking allergy warning"));
        }
    }
}

public class RecipeAllergenCheckResult
{
    public bool HasWarning { get; set; }
    public List<string> Allergens { get; set; } = new();
}

public class AllergyWarningResult
{
    public bool HasWarning { get; set; }
}
