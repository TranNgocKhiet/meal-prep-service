using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RecipeApiController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly ILogger<RecipeApiController> _logger;

    public RecipeApiController(IRecipeService recipeService, ILogger<RecipeApiController> logger)
    {
        _recipeService = recipeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all recipes
    /// </summary>
    /// <returns>List of all recipes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RecipeDto>>> GetAllRecipes()
    {
        try
        {
            var recipes = await _recipeService.GetAllAsync();
            return Ok(recipes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipes");
            return StatusCode(500, new { message = "An error occurred while retrieving recipes" });
        }
    }

    /// <summary>
    /// Get all recipes with ingredients
    /// </summary>
    /// <returns>List of all recipes with their ingredients</returns>
    [HttpGet("with-ingredients")]
    [ProducesResponseType(typeof(IEnumerable<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RecipeDto>>> GetAllRecipesWithIngredients()
    {
        try
        {
            var recipes = await _recipeService.GetAllWithIngredientsAsync();
            return Ok(recipes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipes with ingredients");
            return StatusCode(500, new { message = "An error occurred while retrieving recipes" });
        }
    }

    /// <summary>
    /// Get recipe by ID
    /// </summary>
    /// <param name="id">Recipe ID (GUID)</param>
    /// <returns>Recipe details with ingredients</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RecipeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecipeDto>> GetRecipeById(Guid id)
    {
        try
        {
            var recipe = await _recipeService.GetByIdWithIngredientsAsync(id);
            if (recipe == null)
            {
                return NotFound(new { message = $"Recipe with ID {id} not found" });
            }
            return Ok(recipe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipe {RecipeId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the recipe" });
        }
    }

    /// <summary>
    /// Get recipes by ingredient IDs
    /// </summary>
    /// <param name="ingredientIds">Comma-separated list of ingredient GUIDs</param>
    /// <returns>List of recipes containing the specified ingredients</returns>
    [HttpGet("by-ingredients")]
    [ProducesResponseType(typeof(IEnumerable<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RecipeDto>>> GetRecipesByIngredients([FromQuery] string ingredientIds)
    {
        try
        {
            var ids = ingredientIds.Split(',').Select(Guid.Parse).ToList();
            var recipes = await _recipeService.GetByIngredientsAsync(ids);
            return Ok(recipes);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid ingredient ID format. Please provide comma-separated GUIDs." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipes by ingredients");
            return StatusCode(500, new { message = "An error occurred while retrieving recipes" });
        }
    }

    /// <summary>
    /// Get recipes excluding specific allergens
    /// </summary>
    /// <param name="allergyIds">Comma-separated list of allergy GUIDs to exclude</param>
    /// <returns>List of recipes that don't contain the specified allergens</returns>
    [HttpGet("excluding-allergens")]
    [ProducesResponseType(typeof(IEnumerable<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RecipeDto>>> GetRecipesExcludingAllergens([FromQuery] string allergyIds)
    {
        try
        {
            var ids = allergyIds.Split(',').Select(Guid.Parse).ToList();
            var recipes = await _recipeService.GetExcludingAllergensAsync(ids);
            return Ok(recipes);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid allergy ID format. Please provide comma-separated GUIDs." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recipes excluding allergens");
            return StatusCode(500, new { message = "An error occurred while retrieving recipes" });
        }
    }
}
