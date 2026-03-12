using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/mealplans")]
[Authorize(Roles = "Customer")]
public class MealPlanController : ControllerBase
{
    private readonly IMealPlanService _mealPlanService;
    private readonly IAIMealPlanService _aiMealPlanService;
    private readonly ILogger<MealPlanController> _logger;

    public MealPlanController(
        IMealPlanService mealPlanService,
        IAIMealPlanService aiMealPlanService,
        ILogger<MealPlanController> logger)
    {
        _mealPlanService = mealPlanService;
        _aiMealPlanService = aiMealPlanService;
        _logger = logger;
    }

    [HttpPost("custom")]
    public async Task<ActionResult<ApiResponse<MealPlanDto>>> CreateCustomMealPlan([FromBody] CreateMealPlanDto dto)
    {
        try
        {
            _logger.LogInformation("Received create meal plan request: Name={Name}, Duration={Duration}, StartDate={StartDate}", 
                dto.Name, dto.DurationDays, dto.StartDate);
            
            // Log validation errors if model state is invalid
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                    .ToList();
                
                _logger.LogWarning("Model validation failed: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
                
                var errorMessage = string.Join("; ", errors.SelectMany(e => e.Errors ?? new List<string>()));
                return BadRequest(ApiResponse<MealPlanDto>.ErrorResponse(errorMessage));
            }
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MealPlanDto>.ErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Creating custom meal plan for user {UserId}", userId);
            var mealPlan = await _mealPlanService.CreateCustomMealPlanAsync(dto, userId);
            return Ok(ApiResponse<MealPlanDto>.SuccessResponse(mealPlan, "Meal plan created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating meal plan");
            return BadRequest(ApiResponse<MealPlanDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom meal plan");
            return StatusCode(500, ApiResponse<MealPlanDto>.ErrorResponse("An error occurred while creating meal plan"));
        }
    }

    [HttpPost("ai-generate")]
    public async Task<ActionResult<ApiResponse<MealPlanDto>>> GenerateAIMealPlan([FromBody] CreateMealPlanDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MealPlanDto>.ErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Generating AI meal plan for user {UserId}", userId);
            var mealPlan = await _aiMealPlanService.GenerateAIMealPlanAsync(dto, userId);
            return Ok(ApiResponse<MealPlanDto>.SuccessResponse(mealPlan, "AI meal plan generated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error generating AI meal plan");
            return BadRequest(ApiResponse<MealPlanDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI meal plan");
            return StatusCode(500, ApiResponse<MealPlanDto>.ErrorResponse("An error occurred while generating AI meal plan"));
        }
    }

    [HttpPost("ai-generated")]
    public async Task<ActionResult<ApiResponse<MealPlanDto>>> CreateAiGeneratedMealPlan([FromBody] AiMealPlanRequestDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MealPlanDto>.ErrorResponse("User not authenticated"));
            }

            var mealPlan = await _mealPlanService.CreateAiGeneratedMealPlanAsync(dto, userId);
            return Ok(ApiResponse<MealPlanDto>.SuccessResponse(mealPlan, "AI-generated meal plan created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating AI meal plan");
            return BadRequest(ApiResponse<MealPlanDto>.ErrorResponse(ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "AI service timeout");
            return StatusCode(408, ApiResponse<MealPlanDto>.ErrorResponse("AI service timeout. Please try manual creation."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI-generated meal plan");
            return StatusCode(500, ApiResponse<MealPlanDto>.ErrorResponse("An error occurred while creating AI meal plan"));
        }
    }

    [HttpPut("{mealPlanId}")]
    public async Task<ActionResult<ApiResponse<MealPlanDto>>> UpdateMealPlan(string mealPlanId, [FromBody] UpdateMealPlanDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MealPlanDto>.ErrorResponse("User not authenticated"));
            }

            var mealPlan = await _mealPlanService.UpdateMealPlanAsync(mealPlanId, dto, userId);
            return Ok(ApiResponse<MealPlanDto>.SuccessResponse(mealPlan, "Meal plan updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal plan not found: {MealPlanId}", mealPlanId);
            return NotFound(ApiResponse<MealPlanDto>.ErrorResponse("Meal plan not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating meal plan");
            return BadRequest(ApiResponse<MealPlanDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meal plan");
            return StatusCode(500, ApiResponse<MealPlanDto>.ErrorResponse("An error occurred while updating meal plan"));
        }
    }

    [HttpDelete("{mealPlanId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteMealPlan(string mealPlanId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _mealPlanService.DeleteMealPlanAsync(mealPlanId, userId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Meal plan deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal plan not found: {MealPlanId}", mealPlanId);
            return NotFound(ApiResponse<object>.ErrorResponse("Meal plan not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meal plan");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting meal plan"));
        }
    }

    [HttpGet("{mealPlanId}")]
    public async Task<ActionResult<ApiResponse<MealPlanDto>>> GetMealPlanById(string mealPlanId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MealPlanDto>.ErrorResponse("User not authenticated"));
            }

            var mealPlan = await _mealPlanService.GetMealPlanByIdAsync(mealPlanId, userId);
            if (mealPlan == null)
            {
                return NotFound(ApiResponse<MealPlanDto>.ErrorResponse("Meal plan not found"));
            }

            return Ok(ApiResponse<MealPlanDto>.SuccessResponse(mealPlan, "Meal plan retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plan");
            return StatusCode(500, ApiResponse<MealPlanDto>.ErrorResponse("An error occurred while retrieving meal plan"));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MealPlanDto>>>> GetUserMealPlans()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<MealPlanDto>>.ErrorResponse("User not authenticated"));
            }

            var mealPlans = await _mealPlanService.GetUserMealPlansAsync(userId);
            return Ok(ApiResponse<List<MealPlanDto>>.SuccessResponse(mealPlans, "Meal plans retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user meal plans");
            return StatusCode(500, ApiResponse<List<MealPlanDto>>.ErrorResponse("An error occurred while retrieving meal plans"));
        }
    }

    [HttpPost("{mealPlanId}/set-active")]
    public async Task<ActionResult<ApiResponse<MealPlanDto>>> SetActiveMealPlan(string mealPlanId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<MealPlanDto>.ErrorResponse("User not authenticated"));
            }

            var mealPlan = await _mealPlanService.SetActiveMealPlanAsync(mealPlanId, userId);
            return Ok(ApiResponse<MealPlanDto>.SuccessResponse(mealPlan, "Meal plan set as active successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Meal plan not found: {MealPlanId}", mealPlanId);
            return NotFound(ApiResponse<MealPlanDto>.ErrorResponse("Meal plan not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting meal plan as active");
            return StatusCode(500, ApiResponse<MealPlanDto>.ErrorResponse("An error occurred while setting meal plan as active"));
        }
    }
}
