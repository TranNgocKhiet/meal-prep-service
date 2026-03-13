using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/admin/recipes")]
[Authorize]
public class AdminRecipeController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminRecipeController> _logger;

    public AdminRecipeController(IUnitOfWork unitOfWork, ILogger<AdminRecipeController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AdminRecipeDto>>>> GetAll()
    {
        try
        {
            var recipes = await _unitOfWork.Recipes.GetAllAsync();
            var recipeDtos = recipes.Select(r => new AdminRecipeDto
            {
                Id = r.Id,
                RecipeName = r.RecipeName,
                Instructions = r.Instructions,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList();
            
            return Ok(ApiResponse<IEnumerable<AdminRecipeDto>>.SuccessResponse(recipeDtos, "Recipes retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipes");
            return StatusCode(500, ApiResponse<IEnumerable<AdminRecipeDto>>.ErrorResponse("Failed to retrieve recipes"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AdminRecipeDto>>> GetById(string id)
    {
        try
        {
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(id);
            if (recipe == null)
                return NotFound(ApiResponse<AdminRecipeDto>.ErrorResponse("Recipe not found"));
            
            var recipeDto = new AdminRecipeDto
            {
                Id = recipe.Id,
                RecipeName = recipe.RecipeName,
                Instructions = recipe.Instructions,
                CreatedAt = recipe.CreatedAt,
                UpdatedAt = recipe.UpdatedAt
            };
            
            return Ok(ApiResponse<AdminRecipeDto>.SuccessResponse(recipeDto, "Recipe retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipe {RecipeId}", id);
            return StatusCode(500, ApiResponse<AdminRecipeDto>.ErrorResponse("Failed to retrieve recipe"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminRecipeDto>>> Create([FromBody] CreateRecipeDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<AdminRecipeDto>.ErrorResponse("Invalid recipe data"));
            }

            var recipe = new Recipe
            {
                Id = Guid.NewGuid().ToString(),
                RecipeName = dto.RecipeName,
                Instructions = dto.Instructions,
                CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")),
                UpdatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
            };
            
            await _unitOfWork.Recipes.AddAsync(recipe);
            await _unitOfWork.SaveChangesAsync();
            
            var recipeDto = new AdminRecipeDto
            {
                Id = recipe.Id,
                RecipeName = recipe.RecipeName,
                Instructions = recipe.Instructions,
                CreatedAt = recipe.CreatedAt,
                UpdatedAt = recipe.UpdatedAt
            };
            
            return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, 
                ApiResponse<AdminRecipeDto>.SuccessResponse(recipeDto, "Recipe created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recipe");
            return StatusCode(500, ApiResponse<AdminRecipeDto>.ErrorResponse("Failed to create recipe"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<AdminRecipeDto>>> Update(string id, [FromBody] UpdateRecipeDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<AdminRecipeDto>.ErrorResponse("Invalid recipe data"));
            }

            var existing = await _unitOfWork.Recipes.GetByIdAsync(id);
            if (existing == null)
                return NotFound(ApiResponse<AdminRecipeDto>.ErrorResponse("Recipe not found"));

            existing.RecipeName = dto.RecipeName;
            existing.Instructions = dto.Instructions;
            existing.UpdatedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            await _unitOfWork.Recipes.UpdateAsync(existing);
            await _unitOfWork.SaveChangesAsync();
            
            var recipeDto = new AdminRecipeDto
            {
                Id = existing.Id,
                RecipeName = existing.RecipeName,
                Instructions = existing.Instructions,
                CreatedAt = existing.CreatedAt,
                UpdatedAt = existing.UpdatedAt
            };
            
            return Ok(ApiResponse<AdminRecipeDto>.SuccessResponse(recipeDto, "Recipe updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating recipe {RecipeId}", id);
            return StatusCode(500, ApiResponse<AdminRecipeDto>.ErrorResponse("Failed to update recipe"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        try
        {
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(id);
            if (recipe == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Recipe not found"));

            await _unitOfWork.Recipes.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            
            return Ok(ApiResponse<object>.SuccessResponse(null, "Recipe deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting recipe {RecipeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to delete recipe"));
        }
    }
}

