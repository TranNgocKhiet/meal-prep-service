using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.Web.PresentationLayer.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MenuApiController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenuApiController> _logger;

    public MenuApiController(IMenuService menuService, ILogger<MenuApiController> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    /// <summary>
    /// Get today's menu
    /// </summary>
    /// <returns>Today's menu with meals</returns>
    [HttpGet("today")]
    [ProducesResponseType(typeof(DailyMenuDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyMenuDto>> GetTodayMenu()
    {
        try
        {
            var menu = await _menuService.GetByDateAsync(DateTime.Today);
            if (menu == null)
            {
                return NotFound(new { message = "No menu found for today" });
            }
            return Ok(menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's menu");
            return StatusCode(500, new { message = "An error occurred while retrieving the menu" });
        }
    }

    /// <summary>
    /// Get menu by specific date
    /// </summary>
    /// <param name="date">Date in format yyyy-MM-dd</param>
    /// <returns>Menu for the specified date</returns>
    [HttpGet("date/{date}")]
    [ProducesResponseType(typeof(DailyMenuDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyMenuDto>> GetMenuByDate(DateTime date)
    {
        try
        {
            var menu = await _menuService.GetByDateAsync(date.Date);
            if (menu == null)
            {
                return NotFound(new { message = $"No menu found for {date:yyyy-MM-dd}" });
            }
            return Ok(menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving menu for date {Date}", date);
            return StatusCode(500, new { message = "An error occurred while retrieving the menu" });
        }
    }

    /// <summary>
    /// Get weekly menu (next 7 days from specified start date)
    /// </summary>
    /// <param name="startDate">Optional start date (defaults to today)</param>
    /// <returns>List of menus for the next 7 days</returns>
    [HttpGet("weekly")]
    [ProducesResponseType(typeof(IEnumerable<DailyMenuDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DailyMenuDto>>> GetWeeklyMenu([FromQuery] DateTime? startDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.Today;
            var menus = await _menuService.GetWeeklyMenuAsync(start);
            return Ok(menus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving weekly menu");
            return StatusCode(500, new { message = "An error occurred while retrieving the weekly menu" });
        }
    }
}
