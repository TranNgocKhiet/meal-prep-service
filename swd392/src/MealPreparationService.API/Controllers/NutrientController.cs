using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/nutrients")]
[Authorize(Roles = "Customer")]
public class NutrientController : ControllerBase
{
    private readonly INutrientCalculatorService _nutrientCalculatorService;
    private readonly ILogger<NutrientController> _logger;

    public NutrientController(
        INutrientCalculatorService nutrientCalculatorService,
        ILogger<NutrientController> logger)
    {
        _nutrientCalculatorService = nutrientCalculatorService;
        _logger = logger;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<NutrientCalculationDto>>> CalculateNutrients([FromBody] NutrientRequestDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<NutrientCalculationDto>.ErrorResponse("User not authenticated"));
            }

            var calculation = await _nutrientCalculatorService.CalculateNutrientsAsync(dto, userId);
            return Ok(ApiResponse<NutrientCalculationDto>.SuccessResponse(calculation, "Nutrients calculated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error calculating nutrients");
            return BadRequest(ApiResponse<NutrientCalculationDto>.ErrorResponse(ex.Message));
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "AI service timeout during nutrient calculation");
            return StatusCode(408, ApiResponse<NutrientCalculationDto>.ErrorResponse("AI service timeout. Please try again."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating nutrients");
            return StatusCode(500, ApiResponse<NutrientCalculationDto>.ErrorResponse("An error occurred while calculating nutrients"));
        }
    }

    [HttpPost("{calculationId}/save")]
    public async Task<ActionResult<ApiResponse<object>>> SaveCalculation(string calculationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _nutrientCalculatorService.SaveCalculationAsync(userId, calculationId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Calculation saved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Calculation not found: {CalculationId}", calculationId);
            return NotFound(ApiResponse<object>.ErrorResponse("Calculation not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving calculation");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while saving calculation"));
        }
    }

    [HttpGet("{calculationId}")]
    public async Task<ActionResult<ApiResponse<NutrientCalculationDto>>> GetSavedCalculation(string calculationId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<NutrientCalculationDto>.ErrorResponse("User not authenticated"));
            }

            var calculation = await _nutrientCalculatorService.GetSavedCalculationAsync(calculationId, userId);
            if (calculation == null)
            {
                return NotFound(ApiResponse<NutrientCalculationDto>.ErrorResponse("Calculation not found"));
            }

            return Ok(ApiResponse<NutrientCalculationDto>.SuccessResponse(calculation, "Calculation retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calculation");
            return StatusCode(500, ApiResponse<NutrientCalculationDto>.ErrorResponse("An error occurred while retrieving calculation"));
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<List<NutrientCalculationDto>>>> GetUserCalculations()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<NutrientCalculationDto>>.ErrorResponse("User not authenticated"));
            }

            var calculations = await _nutrientCalculatorService.GetUserCalculationsAsync(userId);
            return Ok(ApiResponse<List<NutrientCalculationDto>>.SuccessResponse(calculations, "Calculation history retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calculation history");
            return StatusCode(500, ApiResponse<List<NutrientCalculationDto>>.ErrorResponse("An error occurred while retrieving calculation history"));
        }
    }
}
