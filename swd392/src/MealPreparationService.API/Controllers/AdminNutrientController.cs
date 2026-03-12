using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/admin/nutrients")]
[Authorize]
public class AdminNutrientController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminNutrientController> _logger;

    public AdminNutrientController(IUnitOfWork unitOfWork, ILogger<AdminNutrientController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Nutrient>>>> GetAll()
    {
        try
        {
            var nutrients = await _unitOfWork.Nutrients.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<Nutrient>> { Success = true, Data = nutrients });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving nutrients");
            return StatusCode(500, new ApiResponse<IEnumerable<Nutrient>> { Success = false });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Nutrient>>> GetById(string id)
    {
        try
        {
            var nutrient = await _unitOfWork.Nutrients.GetByIdAsync(id);
            if (nutrient == null)
                return NotFound(new ApiResponse<Nutrient> { Success = false });
            return Ok(new ApiResponse<Nutrient> { Success = true, Data = nutrient });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving nutrient");
            return StatusCode(500, new ApiResponse<Nutrient> { Success = false });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Nutrient>>> Create([FromBody] Nutrient nutrient)
    {
        try
        {
            nutrient.Id = Guid.NewGuid().ToString();
            await _unitOfWork.Nutrients.AddAsync(nutrient);
            return CreatedAtAction(nameof(GetById), new { id = nutrient.Id }, new ApiResponse<Nutrient> { Success = true, Data = nutrient });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating nutrient");
            return StatusCode(500, new ApiResponse<Nutrient> { Success = false });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Nutrient>>> Update(string id, [FromBody] Nutrient nutrient)
    {
        try
        {
            var existing = await _unitOfWork.Nutrients.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new ApiResponse<Nutrient> { Success = false });

            existing.Name = nutrient.Name;

            await _unitOfWork.Nutrients.UpdateAsync(existing);
            return Ok(new ApiResponse<Nutrient> { Success = true, Data = existing });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating nutrient");
            return StatusCode(500, new ApiResponse<Nutrient> { Success = false });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            var nutrient = await _unitOfWork.Nutrients.GetByIdAsync(id);
            if (nutrient == null)
                return NotFound(new ApiResponse<object> { Success = false });

            await _unitOfWork.Nutrients.DeleteAsync(id);
            return Ok(new ApiResponse<object> { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting nutrient");
            return StatusCode(500, new ApiResponse<object> { Success = false });
        }
    }
}
