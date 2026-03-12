using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/mealtracking")]
[Authorize(Policy = "CustomerOnly")]
public class MealTrackingController : ControllerBase
{
    private readonly IMealTrackingService _mealTrackingService;
    private readonly ILogger<MealTrackingController> _logger;

    public MealTrackingController(
        IMealTrackingService mealTrackingService,
        ILogger<MealTrackingController> logger)
    {
        _mealTrackingService = mealTrackingService;
        _logger = logger;
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<MealDto>>>> GetActiveMeals()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<MealDto>>.ErrorResponse("User not authenticated"));
            }

            var meals = await _mealTrackingService.GetActiveMealsAsync(userId);
            return Ok(ApiResponse<List<MealDto>>.SuccessResponse(meals, "Active meals retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active meals");
            return StatusCode(500, ApiResponse<List<MealDto>>.ErrorResponse("An error occurred while retrieving active meals"));
        }
    }

    [HttpGet("{mealPlanId}/meals/{mealId}/check")]
    public async Task<ActionResult<ApiResponse<MealFinishCheckDto>>> CheckMealIngredients(string mealPlanId, string mealId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User not authenticated for meal ingredient check");
                return Unauthorized(ApiResponse<MealFinishCheckDto>.ErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Checking ingredients for meal {MealId} in plan {MealPlanId} for user {UserId}", 
                mealId, mealPlanId, userId);

            var check = await _mealTrackingService.CheckMealIngredientsAsync(mealPlanId, mealId, userId);
            
            _logger.LogInformation("Ingredient check completed: {Total} total, {Available} available, {Missing} missing", 
                check.TotalIngredients, check.AvailableIngredients, check.MissingIngredients);

            return Ok(ApiResponse<MealFinishCheckDto>.SuccessResponse(check, "Ingredient check completed successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal or meal plan not found: MealPlanId={MealPlanId}, MealId={MealId}", mealPlanId, mealId);
            return NotFound(ApiResponse<MealFinishCheckDto>.ErrorResponse($"Meal or meal plan not found: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking meal ingredients for MealPlanId={MealPlanId}, MealId={MealId}", mealPlanId, mealId);
            return StatusCode(500, ApiResponse<MealFinishCheckDto>.ErrorResponse("An error occurred while checking ingredients"));
        }
    }

    [HttpPost("{mealPlanId}/meals/{mealId}/finish")]
    public async Task<ActionResult<ApiResponse<object>>> MarkMealAsFinished(string mealPlanId, string mealId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _mealTrackingService.MarkMealAsFinishedAsync(mealPlanId, mealId, userId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Meal marked as finished successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal not found: {MealId}", mealId);
            return NotFound(ApiResponse<object>.ErrorResponse("Meal not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error marking meal as finished");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking meal as finished");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while marking meal as finished"));
        }
    }

    [HttpPost("{mealPlanId}/meals/{mealId}/unfinish")]
    public async Task<ActionResult<ApiResponse<object>>> MarkMealAsUnfinished(
        string mealPlanId, 
        string mealId,
        [FromBody] UnfinishMealDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _mealTrackingService.MarkMealAsUnfinishedAsync(mealPlanId, mealId, userId, dto);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Meal marked as unfinished successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal not found: {MealId}", mealId);
            return NotFound(ApiResponse<object>.ErrorResponse("Meal not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error marking meal as unfinished");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking meal as unfinished");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while marking meal as unfinished"));
        }
    }

    [HttpGet("{mealPlanId}/meals/{mealId}/unfinish-check")]
    public async Task<ActionResult<ApiResponse<MealUnfinishCheckDto>>> CheckMealUnfinish(string mealPlanId, string mealId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User not authenticated for meal unfinish check");
                return Unauthorized(ApiResponse<MealUnfinishCheckDto>.ErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Checking ingredients to return for meal {MealId} in plan {MealPlanId} for user {UserId}", 
                mealId, mealPlanId, userId);

            var check = await _mealTrackingService.CheckMealUnfinishAsync(mealPlanId, mealId, userId);
            
            _logger.LogInformation("Unfinish check completed: {Total} ingredients to return", check.TotalIngredients);

            return Ok(ApiResponse<MealUnfinishCheckDto>.SuccessResponse(check, "Unfinish check completed successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal or meal plan not found: MealPlanId={MealPlanId}, MealId={MealId}", mealPlanId, mealId);
            return NotFound(ApiResponse<MealUnfinishCheckDto>.ErrorResponse($"Meal or meal plan not found: {ex.Message}"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error checking meal unfinish");
            return BadRequest(ApiResponse<MealUnfinishCheckDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking meal unfinish for MealPlanId={MealPlanId}, MealId={MealId}", mealPlanId, mealId);
            return StatusCode(500, ApiResponse<MealUnfinishCheckDto>.ErrorResponse("An error occurred while checking ingredients"));
        }
    }

    [HttpGet("meals/{mealId}/status")]
    public async Task<ActionResult<ApiResponse<MealStatusDto>>> GetMealStatus(string mealId)
    {
        try
        {
            var status = await _mealTrackingService.GetMealStatusAsync(mealId);
            return Ok(ApiResponse<MealStatusDto>.SuccessResponse(status, "Meal status retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal not found: {MealId}", mealId);
            return NotFound(ApiResponse<MealStatusDto>.ErrorResponse("Meal not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal status");
            return StatusCode(500, ApiResponse<MealStatusDto>.ErrorResponse("An error occurred while retrieving meal status"));
        }
    }

    [HttpGet("mealplans/{mealPlanId}/progress")]
    public async Task<ActionResult<ApiResponse<MealPlanProgressDto>>> GetMealPlanProgress(string mealPlanId)
    {
        try
        {
            var progress = await _mealTrackingService.GetMealPlanProgressAsync(mealPlanId);
            return Ok(ApiResponse<MealPlanProgressDto>.SuccessResponse(progress, "Meal plan progress retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal plan not found: {MealPlanId}", mealPlanId);
            return NotFound(ApiResponse<MealPlanProgressDto>.ErrorResponse("Meal plan not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plan progress");
            return StatusCode(500, ApiResponse<MealPlanProgressDto>.ErrorResponse("An error occurred while retrieving meal plan progress"));
        }
    }
}
