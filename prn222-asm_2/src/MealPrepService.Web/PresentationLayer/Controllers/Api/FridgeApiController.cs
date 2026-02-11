using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FridgeApiController : ControllerBase
{
    private readonly IFridgeService _fridgeService;
    private readonly ILogger<FridgeApiController> _logger;

    public FridgeApiController(IFridgeService fridgeService, ILogger<FridgeApiController> logger)
    {
        _fridgeService = fridgeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all fridge items for account
    /// </summary>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(IEnumerable<FridgeItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FridgeItemDto>>> GetFridgeItems(Guid accountId)
    {
        try
        {
            var items = await _fridgeService.GetFridgeItemsAsync(accountId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fridge items for account {AccountId}", accountId);
            return StatusCode(500, new { message = "An error occurred while retrieving fridge items" });
        }
    }

    /// <summary>
    /// Get fridge items with pagination
    /// </summary>
    [HttpGet("account/{accountId}/paged")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetFridgeItemsPaged(Guid accountId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var (items, totalCount) = await _fridgeService.GetFridgeItemsPagedAsync(accountId, pageNumber, pageSize);
            return Ok(new { items, totalCount, pageNumber, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged fridge items");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Get expiring items for account
    /// </summary>
    [HttpGet("account/{accountId}/expiring")]
    [ProducesResponseType(typeof(IEnumerable<FridgeItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FridgeItemDto>>> GetExpiringItems(Guid accountId)
    {
        try
        {
            var items = await _fridgeService.GetExpiringItemsAsync(accountId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring items");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Add item to fridge
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FridgeItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FridgeItemDto>> AddItem([FromBody] FridgeItemDto dto)
    {
        try
        {
            var item = await _fridgeService.AddItemAsync(dto);
            return CreatedAtAction(nameof(GetFridgeItems), new { accountId = item.AccountId }, item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding fridge item");
            return StatusCode(500, new { message = "An error occurred while adding the item" });
        }
    }

    /// <summary>
    /// Update item quantity
    /// </summary>
    [HttpPut("{itemId}/quantity")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateQuantity(Guid itemId, [FromBody] float newQuantity)
    {
        try
        {
            await _fridgeService.UpdateItemQuantityAsync(itemId, newQuantity);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item quantity");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Update item expiry date
    /// </summary>
    [HttpPut("{itemId}/expiry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExpiryDate(Guid itemId, [FromBody] DateTime newExpiryDate)
    {
        try
        {
            await _fridgeService.UpdateExpiryDateAsync(itemId, newExpiryDate);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expiry date");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Remove item from fridge
    /// </summary>
    [HttpDelete("{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid itemId)
    {
        try
        {
            await _fridgeService.RemoveItemAsync(itemId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing fridge item");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Generate grocery list from meal plan
    /// </summary>
    [HttpPost("account/{accountId}/grocery-list")]
    [ProducesResponseType(typeof(GroceryListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GroceryListDto>> GenerateGroceryList(Guid accountId, [FromQuery] Guid planId)
    {
        try
        {
            var groceryList = await _fridgeService.GenerateGroceryListAsync(accountId, planId);
            return Ok(groceryList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating grocery list");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Generate grocery list from active meal plan
    /// </summary>
    [HttpPost("account/{accountId}/grocery-list/active")]
    [ProducesResponseType(typeof(GroceryListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GroceryListDto>> GenerateGroceryListFromActivePlan(Guid accountId)
    {
        try
        {
            var groceryList = await _fridgeService.GenerateGroceryListFromActivePlanAsync(accountId);
            return Ok(groceryList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating grocery list from active plan");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
