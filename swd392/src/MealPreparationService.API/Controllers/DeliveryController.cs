using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

/// <summary>
/// Controller for delivery tracking and management operations
/// </summary>
[ApiController]
[Route("api/delivery")]
[Authorize]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<DeliveryController> _logger;

    public DeliveryController(
        IDeliveryService deliveryService,
        ILogger<DeliveryController> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    /// <summary>
    /// Assigns a delivery to delivery personnel (Staff only)
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ApiResponse<DeliveryDetailDto>>> AssignDelivery([FromBody] AssignDeliveryDto dto)
    {
        try
        {
            var delivery = await _deliveryService.AssignDeliveryAsync(dto.OrderId, dto.DeliveryPersonnelId);
            return Ok(ApiResponse<DeliveryDetailDto>.SuccessResponse(delivery, "Delivery assigned successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to assign delivery");
            return BadRequest(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning delivery");
            return StatusCode(500, ApiResponse<DeliveryDetailDto>.ErrorResponse("An error occurred while assigning delivery"));
        }
    }

    /// <summary>
    /// Updates delivery status (Delivery Personnel only)
    /// </summary>
    [HttpPut("{deliveryId}/status")]
    [Authorize(Roles = "Delivery_Personnel")]
    public async Task<ActionResult<ApiResponse<DeliveryDetailDto>>> UpdateStatus(
        string deliveryId,
        [FromBody] UpdateDeliveryStatusDto dto)
    {
        try
        {
            // Verify the delivery belongs to the current user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<DeliveryDetailDto>.ErrorResponse("User ID not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);
            if (delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }

            var updatedDelivery = await _deliveryService.UpdateDeliveryStatusAsync(deliveryId, dto.Status);
            return Ok(ApiResponse<DeliveryDetailDto>.SuccessResponse(updatedDelivery, "Delivery status updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update delivery status");
            return BadRequest(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid status value");
            return BadRequest(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery status");
            return StatusCode(500, ApiResponse<DeliveryDetailDto>.ErrorResponse("An error occurred while updating delivery status"));
        }
    }

    /// <summary>
    /// Updates GPS location for a delivery (Delivery Personnel only)
    /// </summary>
    [HttpPost("{deliveryId}/location")]
    [Authorize(Roles = "Delivery_Personnel")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateLocation(
        string deliveryId,
        [FromBody] UpdateLocationDto dto)
    {
        try
        {
            // Verify the delivery belongs to the current user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User ID not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);
            if (delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }

            var location = new LocationDto
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Timestamp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            };

            await _deliveryService.UpdateLocationAsync(deliveryId, location);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Location updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update location");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid location coordinates");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while updating location"));
        }
    }

    /// <summary>
    /// Gets current location for a delivery (Customer and Delivery Personnel)
    /// </summary>
    [HttpGet("{deliveryId}/location/current")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> GetCurrentLocation(string deliveryId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized(ApiResponse<LocationDto>.ErrorResponse("User information not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);

            // Check authorization: delivery personnel can see their own, customers can see their orders
            if (userRole == "Delivery_Personnel" && delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }
            else if (userRole == "Customer")
            {
                // Verify the order belongs to the customer
                var orderDelivery = await _deliveryService.GetDeliveryByOrderIdAsync(delivery.OrderId);
                if (orderDelivery == null)
                {
                    return NotFound(ApiResponse<LocationDto>.ErrorResponse("Delivery not found"));
                }
                // Additional check would require order service to verify order ownership
            }

            var location = await _deliveryService.GetCurrentLocationAsync(deliveryId);
            if (location == null)
            {
                return NotFound(ApiResponse<LocationDto>.ErrorResponse("No location data available"));
            }

            return Ok(ApiResponse<LocationDto>.SuccessResponse(location, "Current location retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to get current location");
            return NotFound(ApiResponse<LocationDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current location");
            return StatusCode(500, ApiResponse<LocationDto>.ErrorResponse("An error occurred while getting location"));
        }
    }

    /// <summary>
    /// Gets location history for a delivery (Customer and Delivery Personnel)
    /// </summary>
    [HttpGet("{deliveryId}/location/history")]
    public async Task<ActionResult<ApiResponse<List<LocationDto>>>> GetLocationHistory(string deliveryId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized(ApiResponse<List<LocationDto>>.ErrorResponse("User information not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);

            // Check authorization
            if (userRole == "Delivery_Personnel" && delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }

            var history = await _deliveryService.GetLocationHistoryAsync(deliveryId);
            return Ok(ApiResponse<List<LocationDto>>.SuccessResponse(history, "Location history retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to get location history");
            return NotFound(ApiResponse<List<LocationDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location history");
            return StatusCode(500, ApiResponse<List<LocationDto>>.ErrorResponse("An error occurred while getting location history"));
        }
    }

    /// <summary>
    /// Gets all assigned deliveries for the current delivery personnel
    /// </summary>
    [HttpGet("assigned")]
    [Authorize(Roles = "Delivery_Personnel")]
    public async Task<ActionResult<ApiResponse<List<DeliveryDetailDto>>>> GetAssignedDeliveries()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<DeliveryDetailDto>>.ErrorResponse("User ID not found in token"));
            }

            var deliveries = await _deliveryService.GetAssignedDeliveriesAsync(userId);
            return Ok(ApiResponse<List<DeliveryDetailDto>>.SuccessResponse(deliveries, "Assigned deliveries retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned deliveries");
            return StatusCode(500, ApiResponse<List<DeliveryDetailDto>>.ErrorResponse("An error occurred while getting assigned deliveries"));
        }
    }

    /// <summary>
    /// Gets delivery details by ID
    /// </summary>
    [HttpGet("{deliveryId}")]
    public async Task<ActionResult<ApiResponse<DeliveryDetailDto>>> GetDeliveryById(string deliveryId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                return Unauthorized(ApiResponse<DeliveryDetailDto>.ErrorResponse("User information not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);

            // Check authorization
            if (userRole == "Delivery_Personnel" && delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }

            return Ok(ApiResponse<DeliveryDetailDto>.SuccessResponse(delivery, "Delivery retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Delivery not found");
            return NotFound(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery");
            return StatusCode(500, ApiResponse<DeliveryDetailDto>.ErrorResponse("An error occurred while getting delivery"));
        }
    }

    /// <summary>
    /// Gets delivery details by order ID
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<ApiResponse<DeliveryDetailDto>>> GetDeliveryByOrderId(string orderId)
    {
        try
        {
            var delivery = await _deliveryService.GetDeliveryByOrderIdAsync(orderId);
            if (delivery == null)
            {
                return NotFound(ApiResponse<DeliveryDetailDto>.ErrorResponse("No delivery found for this order"));
            }

            return Ok(ApiResponse<DeliveryDetailDto>.SuccessResponse(delivery, "Delivery retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery by order ID");
            return StatusCode(500, ApiResponse<DeliveryDetailDto>.ErrorResponse("An error occurred while getting delivery"));
        }
    }

    /// <summary>
    /// Confirms delivery with signature or photo (Delivery Personnel only)
    /// </summary>
    [HttpPost("{deliveryId}/confirm")]
    [Authorize(Roles = "Delivery_Personnel")]
    public async Task<ActionResult<ApiResponse<DeliveryDetailDto>>> ConfirmDelivery(
        string deliveryId,
        [FromBody] DeliveryConfirmationDto dto)
    {
        try
        {
            // Verify the delivery belongs to the current user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<DeliveryDetailDto>.ErrorResponse("User ID not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);
            if (delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }

            var confirmedDelivery = await _deliveryService.ConfirmDeliveryAsync(deliveryId, dto);
            return Ok(ApiResponse<DeliveryDetailDto>.SuccessResponse(confirmedDelivery, "Delivery confirmed successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to confirm delivery");
            return BadRequest(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid confirmation data");
            return BadRequest(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming delivery");
            return StatusCode(500, ApiResponse<DeliveryDetailDto>.ErrorResponse("An error occurred while confirming delivery"));
        }
    }

    /// <summary>
    /// Marks delivery as failed with reason (Delivery Personnel only)
    /// </summary>
    [HttpPost("{deliveryId}/fail")]
    [Authorize(Roles = "Delivery_Personnel")]
    public async Task<ActionResult<ApiResponse<DeliveryDetailDto>>> MarkDeliveryFailed(
        string deliveryId,
        [FromBody] DeliveryFailureDto dto)
    {
        try
        {
            // Verify the delivery belongs to the current user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<DeliveryDetailDto>.ErrorResponse("User ID not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);
            if (delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }

            var failedDelivery = await _deliveryService.MarkDeliveryFailedAsync(deliveryId, dto.Reason);
            return Ok(ApiResponse<DeliveryDetailDto>.SuccessResponse(failedDelivery, "Delivery marked as failed"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to mark delivery as failed");
            return BadRequest(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid failure reason");
            return BadRequest(ApiResponse<DeliveryDetailDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking delivery as failed");
            return StatusCode(500, ApiResponse<DeliveryDetailDto>.ErrorResponse("An error occurred while marking delivery as failed"));
        }
    }

    /// <summary>
    /// Calculates estimated delivery time (Delivery Personnel only)
    /// </summary>
    [HttpGet("{deliveryId}/estimated-time")]
    [Authorize(Roles = "Delivery_Personnel")]
    public async Task<ActionResult<ApiResponse<EstimatedTimeResult>>> CalculateEstimatedTime(string deliveryId)
    {
        try
        {
            // Verify the delivery belongs to the current user
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<EstimatedTimeResult>.ErrorResponse("User ID not found in token"));
            }

            var delivery = await _deliveryService.GetDeliveryByIdAsync(deliveryId);
            if (delivery.DeliveryPersonnelId != userId)
            {
                return Forbid();
            }

            var estimatedTime = await _deliveryService.CalculateEstimatedTimeAsync(deliveryId);
            var result = new EstimatedTimeResult
            {
                DeliveryId = deliveryId,
                EstimatedTimeMinutes = estimatedTime.TotalMinutes,
                EstimatedTime = estimatedTime.ToString(@"hh\:mm\:ss")
            };

            return Ok(ApiResponse<EstimatedTimeResult>.SuccessResponse(result, "Estimated time calculated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to calculate estimated time");
            return BadRequest(ApiResponse<EstimatedTimeResult>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating estimated time");
            return StatusCode(500, ApiResponse<EstimatedTimeResult>.ErrorResponse("An error occurred while calculating estimated time"));
        }
    }
}

public class EstimatedTimeResult
{
    public string DeliveryId { get; set; } = string.Empty;
    public double EstimatedTimeMinutes { get; set; }
    public string EstimatedTime { get; set; } = string.Empty;
}

