using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/roles")]
public class RoleController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IUnitOfWork unitOfWork, ILogger<RoleController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Role>>>> GetRoles()
    {
        try
        {
            var roles = await _unitOfWork.Roles.GetAllAsync();

            return Ok(new ApiResponse<IEnumerable<Role>>
            {
                Success = true,
                Data = roles,
                Message = "Roles retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new ApiResponse<IEnumerable<Role>>
            {
                Success = false,
                Message = "An error occurred while retrieving roles"
            });
        }
    }
}
