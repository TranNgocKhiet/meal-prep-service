using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MealPlanApiController : ControllerBase
{
    private readonly IMealPlanService _mealPlanService;
    private readonly ILogger<MealPlanApiController> _logger;

    public MealPlanApiController(IMealPlanService mealPlanService, ILogger<MealPlanApiController> logger)
    {
        _mealPlanService = mealPlanService;
        _logger = logger;
    }

    /// <summary>
    /// Generate AI meal plan
    /// </summary>
    [HttpPost("generate-ai")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MealPlanDto>> GenerateAiMealPlan(
        [FromQuery] Guid accountId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? customPlanName = null)
    {
        try
        {
            var mealPlan = await _mealPlanService.GenerateAiMealPlanAsync(accountId, startDate, endDate, customPlanName);
            return CreatedAtAction(nameof(GetById), new { id = mealPlan.Id }, mealPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI meal plan");
            return StatusCode(500, new { message = "An error occurred while generating the meal plan" });
        }
    }

    /// <summary>
    /// Create manual meal plan
    /// </summary>
    [HttpPost("manual")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MealPlanDto>> CreateManual([FromBody] MealPlanDto dto)
    {
        try
        {
            var mealPlan = await _mealPlanService.CreateManualMealPlanAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = mealPlan.Id }, mealPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual meal plan");
            return StatusCode(500, new { message = "An error occurred while creating the meal plan" });
        }
    }

    /// <summary>
    /// Get meal plan by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MealPlanDto>> GetById(Guid id)
    {
        try
        {
            var mealPlan = await _mealPlanService.GetByIdAsync(id);
            if (mealPlan == null)
            {
                return NotFound(new { message = $"Meal plan with ID {id} not found" });
            }
            return Ok(mealPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plan {MealPlanId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the meal plan" });
        }
    }

    /// <summary>
    /// Get meal plans by account ID
    /// </summary>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(IEnumerable<MealPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MealPlanDto>>> GetByAccountId(Guid accountId)
    {
        try
        {
            var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
            return Ok(mealPlans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving meal plans for account {AccountId}", accountId);
            return StatusCode(500, new { message = "An error occurred while retrieving meal plans" });
        }
    }

    /// <summary>
    /// Get active meal plan for account
    /// </summary>
    [HttpGet("account/{accountId}/active")]
    [ProducesResponseType(typeof(MealPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MealPlanDto>> GetActivePlan(Guid accountId)
    {
        try
        {
            var mealPlan = await _mealPlanService.GetActivePlanAsync(accountId);
            if (mealPlan == null)
            {
                return NotFound(new { message = "No active meal plan found" });
            }
            return Ok(mealPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active meal plan for account {AccountId}", accountId);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Add meal to plan
    /// </summary>
    [HttpPost("{id}/meals")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMeal(Guid id, [FromBody] MealDto mealDto)
    {
        try
        {
            await _mealPlanService.AddMealToPlanAsync(id, mealDto);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding meal to plan {MealPlanId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Set meal plan as active
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetActive(Guid id, [FromQuery] Guid accountId)
    {
        try
        {
            await _mealPlanService.SetActivePlanAsync(id, accountId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting meal plan {MealPlanId} as active", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Remove recipe from meal
    /// </summary>
    [HttpDelete("meals/{mealId}/recipes/{recipeId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRecipe(Guid mealId, Guid recipeId, [FromQuery] Guid accountId)
    {
        try
        {
            await _mealPlanService.RemoveRecipeFromMealAsync(mealId, recipeId, accountId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing recipe from meal");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Mark meal as finished/unfinished
    /// </summary>
    [HttpPut("meals/{mealId}/finished")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkMealFinished(Guid mealId, [FromQuery] Guid accountId, [FromQuery] bool finished)
    {
        try
        {
            await _mealPlanService.MarkMealAsFinishedAsync(mealId, accountId, finished);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking meal as finished");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete meal plan
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid accountId)
    {
        try
        {
            await _mealPlanService.DeleteAsync(id, accountId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting meal plan {MealPlanId}", id);
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
