using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<RecipeController> _logger;

    public RecipeController(IRecipeService recipeService, ILogger<RecipeController> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    [HttpPost("search")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<List<RecipeDto>>>> SearchRecipes([FromBody] RecipeSearchDto searchDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<RecipeDto>>.ErrorResponse("User not authenticated"));
            }

            var recipes = await _recipeService.SearchRecipesAsync(searchDto, userId);
            return Ok(ApiResponse<List<RecipeDto>>.SuccessResponse(recipes, "Recipes retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching recipes");
            return StatusCode(500, ApiResponse<List<RecipeDto>>.ErrorResponse("An error occurred while searching recipes"));
        }
    }

    [HttpPost("search/paginated")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<PaginatedResult<RecipeDto>>>> SearchRecipesPaginated(
        [FromBody] RecipeSearchDto searchDto,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<PaginatedResult<RecipeDto>>.ErrorResponse("User not authenticated"));
            }

            var pagination = new PaginationParameters
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var recipes = await _recipeService.SearchRecipesPaginatedAsync(searchDto, userId, pagination);
            return Ok(ApiResponse<PaginatedResult<RecipeDto>>.SuccessResponse(recipes, "Recipes retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching recipes with pagination");
            return StatusCode(500, ApiResponse<PaginatedResult<RecipeDto>>.ErrorResponse("An error occurred while searching recipes"));
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<RecipeDto>>> GetRecipeById(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var recipe = await _recipeService.GetRecipeByIdAsync(id, userId);
            
            if (recipe == null)
            {
                return NotFound(ApiResponse<RecipeDto>.ErrorResponse("Recipe not found"));
            }

            return Ok(ApiResponse<RecipeDto>.SuccessResponse(recipe, "Recipe retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipe by ID: {RecipeId}", id);
            return StatusCode(500, ApiResponse<RecipeDto>.ErrorResponse("An error occurred while retrieving the recipe"));
        }
    }

    [HttpGet("category/{category}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<List<RecipeDto>>>> GetRecipesByCategory(string category)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var recipes = await _recipeService.GetRecipesByCategoryAsync(category, userId);
            return Ok(ApiResponse<List<RecipeDto>>.SuccessResponse(recipes, "Recipes retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recipes by category: {Category}", category);
            return StatusCode(500, ApiResponse<List<RecipeDto>>.ErrorResponse("An error occurred while retrieving recipes"));
        }
    }

    [HttpGet("{id}/allergy-check")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<AllergyCheckResult>>> CheckAllergyCompatibility(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<AllergyCheckResult>.ErrorResponse("User not authenticated"));
            }

            var isCompatible = await _recipeService.CheckAllergyCompatibilityAsync(id, userId);
            var result = new AllergyCheckResult { IsCompatible = isCompatible };
            return Ok(ApiResponse<AllergyCheckResult>.SuccessResponse(result, "Allergy compatibility checked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking allergy compatibility for recipe: {RecipeId}", id);
            return StatusCode(500, ApiResponse<AllergyCheckResult>.ErrorResponse("An error occurred while checking allergy compatibility"));
        }
    }

    [HttpPost("{id}/favorite")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<object>>> AddToFavorites(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _recipeService.AddToFavoritesAsync(id, userId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Recipe added to favorites"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding recipe to favorites: {RecipeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while adding recipe to favorites"));
        }
    }

    [HttpDelete("{id}/favorite")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveFromFavorites(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
            }

            await _recipeService.RemoveFromFavoritesAsync(id, userId);
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Recipe removed from favorites"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing recipe from favorites: {RecipeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("An error occurred while removing recipe from favorites"));
        }
    }

    [HttpGet("favorites")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<ApiResponse<List<RecipeDto>>>> GetUserFavorites()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<RecipeDto>>.ErrorResponse("User not authenticated"));
            }

            var favorites = await _recipeService.GetUserFavoritesAsync(userId);
            return Ok(ApiResponse<List<RecipeDto>>.SuccessResponse(favorites, "Favorites retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user favorites");
            return StatusCode(500, ApiResponse<List<RecipeDto>>.ErrorResponse("An error occurred while retrieving favorites"));
        }
    }
}

public class AllergyCheckResult
{
    public bool IsCompatible { get; set; }
}
