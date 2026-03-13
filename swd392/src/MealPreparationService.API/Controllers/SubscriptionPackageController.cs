using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/subscriptionpackages")]
public class SubscriptionPackageController : ControllerBase
{
    private readonly ISubscriptionPackageService _service;
    private readonly ILogger<SubscriptionPackageController> _logger;

    public SubscriptionPackageController(ISubscriptionPackageService service, ILogger<SubscriptionPackageController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SubscriptionPackageDto>>>> GetAll()
    {
        try
        {
            var packages = await _service.GetAllAsync();
            return Ok(ApiResponse<List<SubscriptionPackageDto>>.SuccessResponse(packages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription packages");
            return StatusCode(500, ApiResponse<List<SubscriptionPackageDto>>.ErrorResponse("Failed to get subscription packages"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SubscriptionPackageDto>>> GetById(string id)
    {
        try
        {
            var package = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<SubscriptionPackageDto>.SuccessResponse(package));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SubscriptionPackageDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription package {Id}", id);
            return StatusCode(500, ApiResponse<SubscriptionPackageDto>.ErrorResponse("Failed to get subscription package"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SubscriptionPackageDto>>> Create([FromBody] CreateSubscriptionPackageDto dto)
    {
        try
        {
            var package = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = package.Id }, ApiResponse<SubscriptionPackageDto>.SuccessResponse(package));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription package");
            return StatusCode(500, ApiResponse<SubscriptionPackageDto>.ErrorResponse("Failed to create subscription package"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubscriptionPackageDto>> Update(string id, [FromBody] UpdateSubscriptionPackageDto dto)
    {
        try
        {
            var package = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<SubscriptionPackageDto>.SuccessResponse(package));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<SubscriptionPackageDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription package {Id}", id);
            return StatusCode(500, ApiResponse<SubscriptionPackageDto>.ErrorResponse("Failed to update subscription package"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Subscription package deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription package {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete subscription package"));
        }
    }
}
