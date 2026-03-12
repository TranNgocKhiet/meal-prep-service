using MealPreparationService.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        _logger.LogInformation("Health check endpoint called");
        
        return Ok(ApiResponse<object>.SuccessResponse(
            new { status = "healthy", timestamp = DateTime.UtcNow },
            "Service is running"));
    }
}
