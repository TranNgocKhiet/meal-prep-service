using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IVnPayService _vnPayService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderService orderService,
        IVnPayService vnPayService,
        ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _vnPayService = vnPayService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<object>>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            var order = await _orderService.CreateOrderAsync(dto, userId);
            
            // If payment method is VNPay, create payment URL
            if (dto.PaymentMethod == "VNPay")
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                // Use the frontend URL for the callback (not the API URL)
                var returnUrl = "http://localhost:5173/payment/callback";
                
                _logger.LogInformation("Creating VNPay payment with return URL: {ReturnUrl}", returnUrl);
                
                var vnpayRequest = new VnPayRequestDto
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    OrderInfo = $"Payment for Order #{order.OrderNumber}",
                    ReturnUrl = returnUrl,
                    IpAddress = ipAddress
                };

                var paymentUrl = await _vnPayService.CreatePaymentUrlAsync(vnpayRequest);
                
                return Ok(ApiResponse<object>.SuccessResponse(
                    new { order, paymentUrl }, 
                    "Order created successfully. Redirect to payment URL."));
            }
            
            return Ok(ApiResponse<object>.SuccessResponse(order, "Order created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating order");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while creating order"));
        }
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrderById(string orderId)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
            return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse("An error occurred while retrieving order"));
        }
    }

    [HttpGet("number/{orderNumber}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrderByNumber(string orderNumber)
    {
        try
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber);
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order retrieved successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found: {OrderNumber}", orderNumber);
            return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse("An error occurred while retrieving order"));
        }
    }

    [HttpGet("user/orders")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetUserOrders()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<OrderDto>>.ErrorResponse("User not authenticated"));
            }

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(ApiResponse<List<OrderDto>>.SuccessResponse(orders, "User orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user orders");
            return StatusCode(500, ApiResponse<List<OrderDto>>.ErrorResponse("An error occurred while retrieving user orders"));
        }
    }

    [HttpGet("user/orders/paginated")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderDto>>>> GetUserOrdersPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<PaginatedResult<OrderDto>>.ErrorResponse("User not authenticated"));
            }

            var pagination = new PaginationParameters
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var orders = await _orderService.GetUserOrdersPaginatedAsync(userId, pagination);
            return Ok(ApiResponse<PaginatedResult<OrderDto>>.SuccessResponse(orders, "User orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user orders with pagination");
            return StatusCode(500, ApiResponse<PaginatedResult<OrderDto>>.ErrorResponse("An error occurred while retrieving user orders"));
        }
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetPendingOrders()
    {
        try
        {
            var orders = await _orderService.GetPendingOrdersAsync();
            return Ok(ApiResponse<List<OrderDto>>.SuccessResponse(orders, "Pending orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending orders");
            return StatusCode(500, ApiResponse<List<OrderDto>>.ErrorResponse("An error occurred while retrieving pending orders"));
        }
    }

    [HttpGet("pending/paginated")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<OrderDto>>>> GetPendingOrdersPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var pagination = new PaginationParameters
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var orders = await _orderService.GetPendingOrdersPaginatedAsync(pagination);
            return Ok(ApiResponse<PaginatedResult<OrderDto>>.SuccessResponse(orders, "Pending orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending orders with pagination");
            return StatusCode(500, ApiResponse<PaginatedResult<OrderDto>>.ErrorResponse("An error occurred while retrieving pending orders"));
        }
    }

    [HttpGet("all")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetAllOrders()
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(ApiResponse<List<OrderDto>>.SuccessResponse(orders, "All orders retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            return StatusCode(500, ApiResponse<List<OrderDto>>.ErrorResponse("An error occurred while retrieving orders"));
        }
    }

    [HttpPost("{orderId}/confirm")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> ConfirmOrder(string orderId)
    {
        try
        {
            var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized(ApiResponse<OrderDto>.ErrorResponse("User not authenticated"));
            }

            var order = await _orderService.ConfirmOrderAsync(orderId, staffId);
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order confirmed successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
            return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error confirming order");
            return BadRequest(ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse("An error occurred while confirming order"));
        }
    }

    [HttpPost("{orderId}/cancel")]
    [Authorize(Roles = "Staff")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CancelOrder(string orderId, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized(ApiResponse<OrderDto>.ErrorResponse("User not authenticated"));
            }

            var order = await _orderService.CancelOrderAsync(orderId, request.Reason, staffId);
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order cancelled successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
            return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error cancelling order");
            return BadRequest(ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse("An error occurred while cancelling order"));
        }
    }

    [HttpPost("{orderId}/update-status")]
    [Authorize(Roles = "Staff,Admin,DeliveryMan,Deliveryman,Delivery_Personnel")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrderStatus(string orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var staffId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(staffId))
            {
                return Unauthorized(ApiResponse<OrderDto>.ErrorResponse("User not authenticated"));
            }

            var order = await _orderService.UpdateOrderStatusAsync(orderId, request.StatusId, staffId);
            return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order status updated successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Order not found: {OrderId}", orderId);
            return NotFound(ApiResponse<OrderDto>.ErrorResponse("Order not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating order status");
            return BadRequest(ApiResponse<OrderDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status");
            return StatusCode(500, ApiResponse<OrderDto>.ErrorResponse("An error occurred while updating order status"));
        }
    }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateOrderStatusRequest
{
    public int StatusId { get; set; }
}
