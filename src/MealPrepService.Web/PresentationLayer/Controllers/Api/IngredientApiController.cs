using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IngredientApiController : ControllerBase
{
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<IngredientApiController> _logger;

    public IngredientApiController(IIngredientService ingredientService, ILogger<IngredientApiController> logger)
    {
        _ingredientService = ingredientService;
        _logger = logger;
    }

    /// <summary>
    /// Get all ingredients
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IngredientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IngredientDto>>> GetAll()
    {
        try
        {
            var ingredients = await _ingredientService.GetAllAsync();
            return Ok(ingredients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ingredients");
            return StatusCode(500, new { message = "An error occurred while retrieving ingredients" });
        }
    }

    /// <summary>
    /// Get ingredient by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IngredientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IngredientDto>> GetById(Guid id)
    {
        try
        {
            var ingredient = await _ingredientService.GetByIdAsync(id);
            if (ingredient == null)
            {
                return NotFound(new { message = $"Ingredient with ID {id} not found" });
            }
            return Ok(ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ingredient {IngredientId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the ingredient" });
        }
    }

    /// <summary>
    /// Get all allergens
    /// </summary>
    [HttpGet("allergens")]
    [ProducesResponseType(typeof(IEnumerable<IngredientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IngredientDto>>> GetAllergens()
    {
        try
        {
            var allergens = await _ingredientService.GetAllergensAsync();
            return Ok(allergens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergens");
            return StatusCode(500, new { message = "An error occurred while retrieving allergens" });
        }
    }

    /// <summary>
    /// Create new ingredient
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IngredientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngredientDto>> Create([FromBody] CreateIngredientDto dto)
    {
        try
        {
            var ingredient = await _ingredientService.CreateIngredientAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = ingredient.Id }, ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ingredient");
            return StatusCode(500, new { message = "An error occurred while creating the ingredient" });
        }
    }

    /// <summary>
    /// Update ingredient
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(IngredientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IngredientDto>> Update(Guid id, [FromBody] UpdateIngredientDto dto)
    {
        try
        {
            var ingredient = await _ingredientService.UpdateIngredientAsync(id, dto);
            return Ok(ingredient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ingredient {IngredientId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the ingredient" });
        }
    }

    /// <summary>
    /// Delete ingredient
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _ingredientService.DeleteIngredientAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ingredient {IngredientId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the ingredient" });
        }
    }
}
