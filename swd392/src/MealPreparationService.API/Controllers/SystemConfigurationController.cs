using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/systemconfigurations")]
public class SystemConfigurationController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SystemConfigurationController> _logger;

    public SystemConfigurationController(IUnitOfWork unitOfWork, ILogger<SystemConfigurationController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SystemConfiguration>>>> GetAll()
    {
        try
        {
            var configs = await _unitOfWork.SystemConfigurations.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<SystemConfiguration>>
            {
                Success = true,
                Data = configs,
                Message = "System configurations retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system configurations");
            return StatusCode(500, new ApiResponse<IEnumerable<SystemConfiguration>>
            {
                Success = false,
                Message = "An error occurred while retrieving system configurations"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SystemConfiguration>>> GetById(string id)
    {
        try
        {
            var config = await _unitOfWork.SystemConfigurations.GetByIdAsync(id);

            if (config == null)
            {
                return NotFound(new ApiResponse<SystemConfiguration>
                {
                    Success = false,
                    Message = "System configuration not found"
                });
            }

            return Ok(new ApiResponse<SystemConfiguration>
            {
                Success = true,
                Data = config,
                Message = "System configuration retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system configuration {Id}", id);
            return StatusCode(500, new ApiResponse<SystemConfiguration>
            {
                Success = false,
                Message = "An error occurred while retrieving the system configuration"
            });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SystemConfiguration>>> Create([FromBody] SystemConfiguration config)
    {
        try
        {
            config.Id = Guid.NewGuid().ToString();
            config.UpdatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            await _unitOfWork.SystemConfigurations.AddAsync(config);

            return CreatedAtAction(nameof(GetById), new { id = config.Id }, new ApiResponse<SystemConfiguration>
            {
                Success = true,
                Data = config,
                Message = "System configuration created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating system configuration");
            return StatusCode(500, new ApiResponse<SystemConfiguration>
            {
                Success = false,
                Message = "An error occurred while creating the system configuration"
            });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<SystemConfiguration>>> Update(string id, [FromBody] SystemConfiguration config)
    {
        try
        {
            var existing = await _unitOfWork.SystemConfigurations.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new ApiResponse<SystemConfiguration>
                {
                    Success = false,
                    Message = "System configuration not found"
                });
            }

            existing.Key = config.Key;
            existing.Value = config.Value;
            existing.DataType = config.DataType;
            existing.Description = config.Description;
            existing.UpdatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            _unitOfWork.SystemConfigurations.UpdateAsync(existing);

            return Ok(new ApiResponse<SystemConfiguration>
            {
                Success = true,
                Data = existing,
                Message = "System configuration updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system configuration {Id}", id);
            return StatusCode(500, new ApiResponse<SystemConfiguration>
            {
                Success = false,
                Message = "An error occurred while updating the system configuration"
            });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            var config = await _unitOfWork.SystemConfigurations.GetByIdAsync(id);
            if (config == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "System configuration not found"
                });
            }

            await _unitOfWork.SystemConfigurations.DeleteAsync(id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "System configuration deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting system configuration {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the system configuration"
            });
        }
    }
}




