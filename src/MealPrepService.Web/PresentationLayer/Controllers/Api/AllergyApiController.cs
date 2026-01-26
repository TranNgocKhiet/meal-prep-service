using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AllergyApiController : ControllerBase
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<AllergyApiController> _logger;

    public AllergyApiController(IAllergyService allergyService, ILogger<AllergyApiController> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all allergies
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AllergyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AllergyDto>>> GetAll()
    {
        try
        {
            var allergies = await _allergyService.GetAllAsync();
            return Ok(allergies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergies");
            return StatusCode(500, new { message = "An error occurred while retrieving allergies" });
        }
    }

    /// <summary>
    /// Get allergy by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AllergyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AllergyDto>> GetById(Guid id)
    {
        try
        {
            var allergy = await _allergyService.GetByIdAsync(id);
            if (allergy == null)
            {
                return NotFound(new { message = $"Allergy with ID {id} not found" });
            }
            return Ok(allergy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergy {AllergyId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the allergy" });
        }
    }

    /// <summary>
    /// Create new allergy
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AllergyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AllergyDto>> Create([FromBody] CreateAllergyDto dto)
    {
        try
        {
            var allergy = await _allergyService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = allergy.Id }, allergy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating allergy");
            return StatusCode(500, new { message = "An error occurred while creating the allergy" });
        }
    }

    /// <summary>
    /// Update allergy
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AllergyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AllergyDto>> Update(Guid id, [FromBody] UpdateAllergyDto dto)
    {
        try
        {
            var allergy = await _allergyService.UpdateAsync(dto);
            return Ok(allergy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergy {AllergyId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the allergy" });
        }
    }

    /// <summary>
    /// Delete allergy
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _allergyService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting allergy {AllergyId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the allergy" });
        }
    }
}
