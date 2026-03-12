using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<CartDto>.ErrorResponse("User not authenticated"));
            }

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(ApiResponse<CartDto>.SuccessResponse(cart, "Cart retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse("An error occurred while retrieving cart"));
        }
    }

    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddItemToCart([FromBody] AddCartItemDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<CartDto>.ErrorResponse("User not authenticated"));
            }

            var cart = await _cartService.AddItemToCartAsync(userId, dto);
            return Ok(ApiResponse<CartDto>.SuccessResponse(cart, "Item added to cart successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Menu meal not found");
            return NotFound(ApiResponse<CartDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error adding item to cart");
            return BadRequest(ApiResponse<CartDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse("An error occurred while adding item to cart"));
        }
    }

    [HttpPut("items/{cartItemId}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(string cartItemId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<CartDto>.ErrorResponse("User not authenticated"));
            }

            var cart = await _cartService.UpdateCartItemAsync(userId, cartItemId, dto.Quantity);
            return Ok(ApiResponse<CartDto>.SuccessResponse(cart, "Cart item updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Cart item not found");
            return NotFound(ApiResponse<CartDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating cart item");
            return BadRequest(ApiResponse<CartDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            return StatusCode(500, ApiResponse<CartDto>.ErrorResponse("An error occurred while updating cart item"));
        }
    }

    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveCartItem(string cartItemId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _cartService.RemoveCartItemAsync(userId, cartItemId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Cart item removed successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Cart item not found");
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while removing cart item"));
        }
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> ClearCart()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _cartService.ClearCartAsync(userId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Cart cleared successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while clearing cart"));
        }
    }
}
