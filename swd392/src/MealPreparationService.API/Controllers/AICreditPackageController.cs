using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/aicreditpackages")]
public class AICreditPackageController : ControllerBase
{
    private readonly IAICreditPackageService _service;
    private readonly ILogger<AICreditPackageController> _logger;

    public AICreditPackageController(IAICreditPackageService service, ILogger<AICreditPackageController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AICreditPackageDto>>>> GetAll()
    {
        try
        {
            var packages = await _service.GetAllAsync();
            return Ok(ApiResponse<List<AICreditPackageDto>>.SuccessResponse(packages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI credit packages");
            return StatusCode(500, ApiResponse<List<AICreditPackageDto>>.ErrorResponse("Failed to get AI credit packages"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AICreditPackageDto>>> GetById(string id)
    {
        try
        {
            var package = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<AICreditPackageDto>.SuccessResponse(package));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AICreditPackageDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI credit package {Id}", id);
            return StatusCode(500, ApiResponse<AICreditPackageDto>.ErrorResponse("Failed to get AI credit package"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AICreditPackageDto>>> Create([FromBody] CreateAICreditPackageDto dto)
    {
        try
        {
            var package = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = package.Id }, ApiResponse<AICreditPackageDto>.SuccessResponse(package));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI credit package");
            return StatusCode(500, ApiResponse<AICreditPackageDto>.ErrorResponse("Failed to create AI credit package"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AICreditPackageDto>>> Update(string id, [FromBody] UpdateAICreditPackageDto dto)
    {
        try
        {
            var package = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<AICreditPackageDto>.SuccessResponse(package));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AICreditPackageDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating AI credit package {Id}", id);
            return StatusCode(500, ApiResponse<AICreditPackageDto>.ErrorResponse("Failed to update AI credit package"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "AI credit package deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting AI credit package {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete AI credit package"));
        }
    }
}
