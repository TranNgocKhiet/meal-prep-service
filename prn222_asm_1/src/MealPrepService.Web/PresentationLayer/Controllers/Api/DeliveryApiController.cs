using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DeliveryApiController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;
    private readonly ILogger<DeliveryApiController> _logger;

    public DeliveryApiController(IDeliveryService deliveryService, ILogger<DeliveryApiController> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    /// <summary>
    /// Create delivery schedule
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DeliveryScheduleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeliveryScheduleDto>> CreateDeliverySchedule([FromQuery] Guid orderId, [FromBody] DeliveryScheduleDto dto)
    {
        try
        {
            var delivery = await _deliveryService.CreateDeliveryScheduleAsync(orderId, dto);
            return CreatedAtAction(nameof(GetByAccount), new { accountId = delivery.Order?.AccountId ?? Guid.Empty }, delivery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery schedule");
            return StatusCode(500, new { message = "An error occurred while creating the delivery schedule" });
        }
    }

    /// <summary>
    /// Get deliveries by account ID
    /// </summary>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(IEnumerable<DeliveryScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeliveryScheduleDto>>> GetByAccount(Guid accountId)
    {
        try
        {
            var deliveries = await _deliveryService.GetByAccountIdAsync(accountId);
            return Ok(deliveries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deliveries for account {AccountId}", accountId);
            return StatusCode(500, new { message = "An error occurred while retrieving deliveries" });
        }
    }

    /// <summary>
    /// Get deliveries by delivery man ID
    /// </summary>
    [HttpGet("deliveryman/{deliveryManId}")]
    [ProducesResponseType(typeof(IEnumerable<DeliveryScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeliveryScheduleDto>>> GetByDeliveryMan(Guid deliveryManId)
    {
        try
        {
            var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);
            return Ok(deliveries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deliveries for delivery man {DeliveryManId}", deliveryManId);
            return StatusCode(500, new { message = "An error occurred while retrieving deliveries" });
        }
    }

    /// <summary>
    /// Complete delivery
    /// </summary>
    [HttpPost("{deliveryId}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteDelivery(Guid deliveryId)
    {
        try
        {
            await _deliveryService.CompleteDeliveryAsync(deliveryId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing delivery {DeliveryId}", deliveryId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update delivery time
    /// </summary>
    [HttpPut("{deliveryId}/time")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDeliveryTime(Guid deliveryId, [FromBody] DateTime newTime)
    {
        try
        {
            await _deliveryService.UpdateDeliveryTimeAsync(deliveryId, newTime);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery time");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
