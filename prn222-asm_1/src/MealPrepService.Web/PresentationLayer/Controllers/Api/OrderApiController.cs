using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrderApiController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderApiController> _logger;

    public OrderApiController(IOrderService orderService, ILogger<OrderApiController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromQuery] Guid accountId, [FromBody] List<OrderItemDto> items)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(accountId, items);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { message = "An error occurred while creating the order" });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found" });
            }
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the order" });
        }
    }

    /// <summary>
    /// Get orders by account ID
    /// </summary>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetByAccountId(Guid accountId)
    {
        try
        {
            var orders = await _orderService.GetByAccountIdAsync(accountId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for account {AccountId}", accountId);
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Process payment for an order
    /// </summary>
    [HttpPost("{id}/payment")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> ProcessPayment(Guid id, [FromBody] string paymentMethod)
    {
        try
        {
            var order = await _orderService.ProcessPaymentAsync(id, paymentMethod);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while processing payment" });
        }
    }

    /// <summary>
    /// Confirm cash payment (Delivery Man only)
    /// </summary>
    [HttpPost("{id}/confirm-cash")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> ConfirmCashPayment(Guid id, [FromQuery] Guid deliveryManId)
    {
        try
        {
            var order = await _orderService.ConfirmCashPaymentAsync(id, deliveryManId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming cash payment for order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        try
        {
            await _orderService.UpdateOrderStatusAsync(id, status);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Process VNPay callback
    /// </summary>
    [HttpPost("vnpay-callback")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDto>> VnpayCallback([FromBody] VnpayCallbackDto callbackDto)
    {
        try
        {
            var order = await _orderService.ProcessVnpayCallbackAsync(callbackDto);
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay callback");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
