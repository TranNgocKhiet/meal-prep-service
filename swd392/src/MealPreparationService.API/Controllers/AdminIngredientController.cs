using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/admin/ingredients")]
[Authorize]
public class AdminIngredientController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminIngredientController> _logger;

    public AdminIngredientController(IUnitOfWork unitOfWork, ILogger<AdminIngredientController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Ingredient>>>> GetAll()
    {
        try
        {
            var ingredients = await _unitOfWork.Ingredients.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<Ingredient>>
            {
                Success = true,
                Data = ingredients
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ingredients");
            return StatusCode(500, new ApiResponse<IEnumerable<Ingredient>>
            {
                Success = false,
                Message = "An error occurred"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Ingredient>>> GetById(string id)
    {
        try
        {
            var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(id);
            if (ingredient == null)
                return NotFound(new ApiResponse<Ingredient> { Success = false, Message = "Not found" });

            return Ok(new ApiResponse<Ingredient> { Success = true, Data = ingredient });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ingredient");
            return StatusCode(500, new ApiResponse<Ingredient> { Success = false });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Ingredient>>> Create([FromBody] Ingredient ingredient)
    {
        try
        {
            ingredient.Id = Guid.NewGuid().ToString();
            await _unitOfWork.Ingredients.AddAsync(ingredient);
            return CreatedAtAction(nameof(GetById), new { id = ingredient.Id }, new ApiResponse<Ingredient> { Success = true, Data = ingredient });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ingredient");
            return StatusCode(500, new ApiResponse<Ingredient> { Success = false });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Ingredient>>> Update(string id, [FromBody] Ingredient ingredient)
    {
        try
        {
            var existing = await _unitOfWork.Ingredients.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new ApiResponse<Ingredient> { Success = false });

            existing.Name = ingredient.Name;
            existing.Unit = ingredient.Unit;
            existing.ImageUrl = ingredient.ImageUrl;

            await _unitOfWork.Ingredients.UpdateAsync(existing);
            return Ok(new ApiResponse<Ingredient> { Success = true, Data = existing });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ingredient");
            return StatusCode(500, new ApiResponse<Ingredient> { Success = false });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            var ingredient = await _unitOfWork.Ingredients.GetByIdAsync(id);
            if (ingredient == null)
                return NotFound(new ApiResponse<object> { Success = false });

            await _unitOfWork.Ingredients.DeleteAsync(id);
            return Ok(new ApiResponse<object> { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ingredient");
            return StatusCode(500, new ApiResponse<object> { Success = false });
        }
    }
}
