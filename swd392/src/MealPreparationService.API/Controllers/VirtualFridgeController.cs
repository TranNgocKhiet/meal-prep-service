using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/fridge")]
[Authorize(Policy = "CustomerOnly")]
public class VirtualFridgeController : ControllerBase
{
    private readonly IVirtualFridgeService _fridgeService;
    private readonly ILogger<VirtualFridgeController> _logger;

    public VirtualFridgeController(
        IVirtualFridgeService fridgeService,
        ILogger<VirtualFridgeController> logger)
    {
        _fridgeService = fridgeService;
        _logger = logger;
    }

    /// <summary>
    /// Add a new item to the virtual fridge
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<FridgeItemDto>>> AddItem([FromBody] AddFridgeItemDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<FridgeItemDto>.ErrorResponse("User not authenticated"));
            }

            var result = await _fridgeService.AddItemAsync(dto, userId);
            return Ok(ApiResponse<FridgeItemDto>.SuccessResponse(result, "Item added to fridge successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to add fridge item due to business rule violation");
            return BadRequest(ApiResponse<FridgeItemDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed to add fridge item due to invalid input");
            return BadRequest(ApiResponse<FridgeItemDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error adding fridge item");
            return StatusCode(500, ApiResponse<FridgeItemDto>.ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Update an existing fridge item
    /// </summary>
    [HttpPut("{itemId}")]
    public async Task<ActionResult<ApiResponse<FridgeItemDto>>> UpdateItem(
        string itemId,
        [FromBody] UpdateFridgeItemDto dto)
    {
        try
        {
            var result = await _fridgeService.UpdateItemAsync(itemId, dto);
            return Ok(ApiResponse<FridgeItemDto>.SuccessResponse(result, "Item updated successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed to update fridge item");
            return BadRequest(ApiResponse<FridgeItemDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating fridge item");
            return StatusCode(500, ApiResponse<FridgeItemDto>.ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Delete a fridge item
    /// </summary>
    [HttpDelete("{itemId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteItem(string itemId)
    {
        try
        {
            await _fridgeService.DeleteItemAsync(itemId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Item deleted successfully"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed to delete fridge item");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting fridge item");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Get all fridge items for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FridgeItemDto>>>> GetFridgeItems(
        [FromQuery] bool includeExpired = true)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<FridgeItemDto>>.ErrorResponse("User not authenticated"));
            }

            var result = await _fridgeService.GetUserFridgeItemsAsync(userId, includeExpired);
            return Ok(ApiResponse<List<FridgeItemDto>>.SuccessResponse(result, "Fridge items retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving fridge items");
            return StatusCode(500, ApiResponse<List<FridgeItemDto>>.ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Get items expiring within the specified number of days
    /// </summary>
    [HttpGet("expiring")]
    public async Task<ActionResult<ApiResponse<List<FridgeItemDto>>>> GetExpiringItems(
        [FromQuery] int daysThreshold = 7)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<FridgeItemDto>>.ErrorResponse("User not authenticated"));
            }

            if (daysThreshold < 0)
            {
                return BadRequest(ApiResponse<List<FridgeItemDto>>.ErrorResponse("Days threshold must be non-negative"));
            }

            var result = await _fridgeService.GetExpiringItemsAsync(userId, daysThreshold);
            return Ok(ApiResponse<List<FridgeItemDto>>.SuccessResponse(
                result, 
                $"Found {result.Count} items expiring within {daysThreshold} days"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving expiring items");
            return StatusCode(500, ApiResponse<List<FridgeItemDto>>.ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Check if sufficient quantity exists for a specific ingredient
    /// </summary>
    [HttpGet("check-quantity")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckSufficientQuantity(
        [FromQuery] string ingredientId,
        [FromQuery] decimal quantity)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));
            }

            if (string.IsNullOrEmpty(ingredientId))
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse("Ingredient ID is required"));
            }

            if (quantity <= 0)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse("Quantity must be greater than zero"));
            }

            var result = await _fridgeService.HasSufficientQuantityAsync(userId, ingredientId, quantity);
            return Ok(ApiResponse<bool>.SuccessResponse(result, 
                result ? "Sufficient quantity available" : "Insufficient quantity"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking quantity");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Generate grocery list based on active meal plan and current fridge items
    /// </summary>
    [HttpGet("grocery-list")]
    public async Task<ActionResult<ApiResponse<GroceryListDto>>> GenerateGroceryList()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<GroceryListDto>.ErrorResponse("User not authenticated"));
            }

            var result = await _fridgeService.GenerateGroceryListAsync(userId);
            return Ok(ApiResponse<GroceryListDto>.SuccessResponse(result, "Grocery list generated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating grocery list");
            return StatusCode(500, ApiResponse<GroceryListDto>.ErrorResponse("An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Purchase grocery items and add them to the fridge
    /// </summary>
    [HttpPost("purchase")]
    public async Task<ActionResult<ApiResponse<List<FridgeItemDto>>>> PurchaseGroceryItems(
        [FromBody] PurchaseGroceryListDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<FridgeItemDto>>.ErrorResponse("User not authenticated"));
            }

            var result = await _fridgeService.PurchaseGroceryItemsAsync(userId, dto);
            return Ok(ApiResponse<List<FridgeItemDto>>.SuccessResponse(
                result, 
                $"Successfully purchased {result.Count} items"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed to purchase grocery items");
            return BadRequest(ApiResponse<List<FridgeItemDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error purchasing grocery items");
            return StatusCode(500, ApiResponse<List<FridgeItemDto>>.ErrorResponse("An unexpected error occurred"));
        }
    }
}
