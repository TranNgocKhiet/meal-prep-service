using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/delivery-schedules")]
[Authorize(Roles = "Admin,Staff")]
public class DeliveryScheduleController : ControllerBase
{
    private readonly IDeliveryScheduleService _deliveryScheduleService;
    private readonly ILogger<DeliveryScheduleController> _logger;

    public DeliveryScheduleController(
        IDeliveryScheduleService deliveryScheduleService,
        ILogger<DeliveryScheduleController> logger)
    {
        _deliveryScheduleService = deliveryScheduleService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeliveryScheduleDto>>> CreateDeliverySchedule(
        [FromBody] CreateDeliveryScheduleDto dto)
    {
        try
        {
            var schedule = await _deliveryScheduleService.CreateDeliveryScheduleAsync(dto);
            return Ok(ApiResponse<DeliveryScheduleDto>.SuccessResponse(schedule, "Delivery schedule created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found while creating delivery schedule");
            return NotFound(ApiResponse<DeliveryScheduleDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating delivery schedule");
            return BadRequest(ApiResponse<DeliveryScheduleDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery schedule");
            return StatusCode(500, ApiResponse<DeliveryScheduleDto>.ErrorResponse("An error occurred while creating delivery schedule"));
        }
    }

    [HttpPut("{scheduleId}")]
    public async Task<ActionResult<ApiResponse<DeliveryScheduleDto>>> UpdateDeliverySchedule(
        string scheduleId,
        [FromBody] UpdateDeliveryScheduleDto dto)
    {
        try
        {
            var schedule = await _deliveryScheduleService.UpdateDeliveryScheduleAsync(scheduleId, dto);
            return Ok(ApiResponse<DeliveryScheduleDto>.SuccessResponse(schedule, "Delivery schedule updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Delivery schedule not found: {ScheduleId}", scheduleId);
            return NotFound(ApiResponse<DeliveryScheduleDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating delivery schedule");
            return BadRequest(ApiResponse<DeliveryScheduleDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery schedule");
            return StatusCode(500, ApiResponse<DeliveryScheduleDto>.ErrorResponse("An error occurred while updating delivery schedule"));
        }
    }

    [HttpDelete("{scheduleId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDeliverySchedule(string scheduleId)
    {
        try
        {
            await _deliveryScheduleService.DeleteDeliveryScheduleAsync(scheduleId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Delivery schedule deleted successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Delivery schedule not found: {ScheduleId}", scheduleId);
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting delivery schedule");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while deleting delivery schedule"));
        }
    }

    [HttpGet("{scheduleId}")]
    public async Task<ActionResult<ApiResponse<DeliveryScheduleDto>>> GetDeliveryScheduleById(string scheduleId)
    {
        try
        {
            var schedule = await _deliveryScheduleService.GetDeliveryScheduleByIdAsync(scheduleId);
            if (schedule == null)
            {
                return NotFound(ApiResponse<DeliveryScheduleDto>.ErrorResponse("Delivery schedule not found"));
            }

            return Ok(ApiResponse<DeliveryScheduleDto>.SuccessResponse(schedule, "Delivery schedule retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery schedule");
            return StatusCode(500, ApiResponse<DeliveryScheduleDto>.ErrorResponse("An error occurred while retrieving delivery schedule"));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DeliveryScheduleDto>>>> GetAllDeliverySchedules()
    {
        try
        {
            var schedules = await _deliveryScheduleService.GetAllDeliverySchedulesAsync();
            return Ok(ApiResponse<List<DeliveryScheduleDto>>.SuccessResponse(schedules, "Delivery schedules retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery schedules");
            return StatusCode(500, ApiResponse<List<DeliveryScheduleDto>>.ErrorResponse("An error occurred while retrieving delivery schedules"));
        }
    }

    [HttpGet("driver/{driverId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<DeliveryScheduleDto>>>> GetDeliverySchedulesByDriver(string driverId)
    {
        try
        {
            // Check if user is authenticated
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(ApiResponse<List<DeliveryScheduleDto>>.ErrorResponse("User is not authenticated"));
            }

            // Check if user has the correct role
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var allowedRoles = new[] { "Admin", "Staff", "Deliveryman", "DeliveryMan", "Delivery_Personnel" };
            
            if (string.IsNullOrEmpty(userRole) || !allowedRoles.Contains(userRole))
            {
                _logger.LogWarning("User with role {Role} attempted to access driver schedules", userRole);
                return Forbid();
            }

            var schedules = await _deliveryScheduleService.GetDeliverySchedulesByDriverAsync(driverId);
            return Ok(ApiResponse<List<DeliveryScheduleDto>>.SuccessResponse(schedules, "Driver delivery schedules retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving driver delivery schedules");
            return StatusCode(500, ApiResponse<List<DeliveryScheduleDto>>.ErrorResponse("An error occurred while retrieving driver delivery schedules"));
        }
    }

    [HttpGet("drivers/available")]
    public async Task<ActionResult<ApiResponse<List<DriverDto>>>> GetAvailableDrivers()
    {
        try
        {
            var drivers = await _deliveryScheduleService.GetAvailableDriversAsync();
            return Ok(ApiResponse<List<DriverDto>>.SuccessResponse(drivers, "Available drivers retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available drivers");
            return StatusCode(500, ApiResponse<List<DriverDto>>.ErrorResponse("An error occurred while retrieving available drivers"));
        }
    }
}
