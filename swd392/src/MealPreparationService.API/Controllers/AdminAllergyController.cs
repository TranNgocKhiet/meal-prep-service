using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/admin/allergies")]
[Authorize]
public class AdminAllergyController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminAllergyController> _logger;

    public AdminAllergyController(IUnitOfWork unitOfWork, ILogger<AdminAllergyController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Allergy>>>> GetAll()
    {
        try
        {
            var allergies = await _unitOfWork.Allergies.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<Allergy>>
            {
                Success = true,
                Data = allergies,
                Message = "Allergies retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergies");
            return StatusCode(500, new ApiResponse<IEnumerable<Allergy>>
            {
                Success = false,
                Message = "An error occurred while retrieving allergies"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Allergy>>> GetById(string id)
    {
        try
        {
            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (allergy == null)
            {
                return NotFound(new ApiResponse<Allergy>
                {
                    Success = false,
                    Message = "Allergy not found"
                });
            }

            return Ok(new ApiResponse<Allergy>
            {
                Success = true,
                Data = allergy,
                Message = "Allergy retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergy {Id}", id);
            return StatusCode(500, new ApiResponse<Allergy>
            {
                Success = false,
                Message = "An error occurred while retrieving the allergy"
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Allergy>>> Create([FromBody] Allergy allergy)
    {
        try
        {
            allergy.Id = Guid.NewGuid().ToString();
            await _unitOfWork.Allergies.AddAsync(allergy);

            return CreatedAtAction(nameof(GetById), new { id = allergy.Id }, new ApiResponse<Allergy>
            {
                Success = true,
                Data = allergy,
                Message = "Allergy created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating allergy");
            return StatusCode(500, new ApiResponse<Allergy>
            {
                Success = false,
                Message = "An error occurred while creating the allergy"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Allergy>>> Update(string id, [FromBody] Allergy allergy)
    {
        try
        {
            var existing = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new ApiResponse<Allergy>
                {
                    Success = false,
                    Message = "Allergy not found"
                });
            }

            existing.Name = allergy.Name;

            await _unitOfWork.Allergies.UpdateAsync(existing);

            return Ok(new ApiResponse<Allergy>
            {
                Success = true,
                Data = existing,
                Message = "Allergy updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergy {Id}", id);
            return StatusCode(500, new ApiResponse<Allergy>
            {
                Success = false,
                Message = "An error occurred while updating the allergy"
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            var allergy = await _unitOfWork.Allergies.GetByIdAsync(id);
            if (allergy == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Allergy not found"
                });
            }

            await _unitOfWork.Allergies.DeleteAsync(id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Allergy deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting allergy {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the allergy"
            });
        }
    }
}
