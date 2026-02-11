using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthProfileApiController : ControllerBase
{
    private readonly IHealthProfileService _healthProfileService;
    private readonly ILogger<HealthProfileApiController> _logger;

    public HealthProfileApiController(IHealthProfileService healthProfileService, ILogger<HealthProfileApiController> logger)
    {
        _healthProfileService = healthProfileService;
        _logger = logger;
    }

    /// <summary>
    /// Get health profile by account ID
    /// </summary>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(HealthProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthProfileDto>> GetByAccountId(Guid accountId)
    {
        try
        {
            var profile = await _healthProfileService.GetByAccountIdAsync(accountId);
            if (profile == null)
            {
                return NotFound(new { message = $"Health profile for account {accountId} not found" });
            }
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health profile for account {AccountId}", accountId);
            return StatusCode(500, new { message = "An error occurred while retrieving the health profile" });
        }
    }

    /// <summary>
    /// Create or update health profile
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(HealthProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HealthProfileDto>> CreateOrUpdate([FromBody] HealthProfileDto dto)
    {
        try
        {
            var profile = await _healthProfileService.CreateOrUpdateAsync(dto);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating health profile");
            return StatusCode(500, new { message = "An error occurred while saving the health profile" });
        }
    }

    /// <summary>
    /// Add allergy to health profile
    /// </summary>
    [HttpPost("{profileId}/allergies/{allergyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAllergy(Guid profileId, Guid allergyId)
    {
        try
        {
            await _healthProfileService.AddAllergyAsync(profileId, allergyId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding allergy to health profile");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    /// <summary>
    /// Remove allergy from health profile
    /// </summary>
    [HttpDelete("{profileId}/allergies/{allergyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAllergy(Guid profileId, Guid allergyId)
    {
        try
        {
            await _healthProfileService.RemoveAllergyAsync(profileId, allergyId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing allergy from health profile");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
