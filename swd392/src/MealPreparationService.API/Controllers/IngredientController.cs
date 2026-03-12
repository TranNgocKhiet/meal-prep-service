using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/ingredients")]
[Authorize]
public class IngredientController : ControllerBase
{
    private readonly IIngredientService _ingredientService;
    private readonly ILogger<IngredientController> _logger;

    public IngredientController(IIngredientService ingredientService, ILogger<IngredientController> logger)
    {
        _ingredientService = ingredientService;
        _logger = logger;
    }

    [HttpGet("search")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<List<IngredientDto>>>> SearchIngredientsByTerm([FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Ok(ApiResponse<List<IngredientDto>>.SuccessResponse(new List<IngredientDto>(), "Search term too short"));
            }

            // Search ingredients directly without highlighting
            var ingredients = await _ingredientService.SearchIngredientsWithoutHighlightAsync(term);
            return Ok(ApiResponse<List<IngredientDto>>.SuccessResponse(ingredients, "Ingredients retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ingredients by term");
            return StatusCode(500, ApiResponse<List<IngredientDto>>.ErrorResponse("An error occurred while searching ingredients"));
        }
    }

    [HttpPost("search")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<List<IngredientDto>>>> SearchIngredients([FromBody] IngredientSearchDto searchDto)
    {
        try
        {
            var ingredients = await _ingredientService.SearchIngredientsAsync(searchDto);
            return Ok(ApiResponse<List<IngredientDto>>.SuccessResponse(ingredients, "Ingredients retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ingredients");
            return StatusCode(500, ApiResponse<List<IngredientDto>>.ErrorResponse("An error occurred while searching ingredients"));
        }
    }

    [HttpPost("search/paginated")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<IngredientDto>>>> SearchIngredientsPaginated(
        [FromBody] IngredientSearchDto searchDto,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var pagination = new PaginationParameters
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var ingredients = await _ingredientService.SearchIngredientsPaginatedAsync(searchDto, pagination);
            return Ok(ApiResponse<PaginatedResult<IngredientDto>>.SuccessResponse(ingredients, "Ingredients retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching ingredients with pagination");
            return StatusCode(500, ApiResponse<PaginatedResult<IngredientDto>>.ErrorResponse("An error occurred while searching ingredients"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<IngredientDto>>> GetIngredientById(string id)
    {
        try
        {
            var ingredient = await _ingredientService.GetIngredientByIdAsync(id);
            
            if (ingredient == null)
            {
                return NotFound(ApiResponse<IngredientDto>.ErrorResponse("Ingredient not found"));
            }

            return Ok(ApiResponse<IngredientDto>.SuccessResponse(ingredient, "Ingredient retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingredient by ID: {IngredientId}", id);
            return StatusCode(500, ApiResponse<IngredientDto>.ErrorResponse("An error occurred while retrieving the ingredient"));
        }
    }

}
