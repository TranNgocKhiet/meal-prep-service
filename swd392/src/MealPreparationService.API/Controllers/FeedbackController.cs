using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/feedbacks")]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(IFeedbackService feedbackService, ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> CreateFeedback([FromBody] CreateFeedbackDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                var errorMessage = string.Join("; ", errors.SelectMany(e => e.Errors ?? new List<string>()));
                return BadRequest(ApiResponse<FeedbackDto>.ErrorResponse(errorMessage));
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<FeedbackDto>.ErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Customer {UserId} creating feedback", userId);
            var feedback = await _feedbackService.CreateFeedbackAsync(dto, userId);
            return Ok(ApiResponse<FeedbackDto>.SuccessResponse(feedback, "Feedback created successfully"));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer not found while creating feedback");
            return NotFound(ApiResponse<FeedbackDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback");
            return StatusCode(500, ApiResponse<FeedbackDto>.ErrorResponse("An error occurred while creating feedback"));
        }
    }

    [HttpGet("{feedbackId}")]
    public async Task<ActionResult<ApiResponse<FeedbackDto>>> GetFeedbackById(string feedbackId)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<FeedbackDto>.ErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Getting feedback {FeedbackId}", feedbackId);
            var feedback = await _feedbackService.GetFeedbackByIdAsync(feedbackId);

            if (feedback == null)
            {
                return NotFound(ApiResponse<FeedbackDto>.ErrorResponse("Feedback not found"));
            }

            // Check authorization: customer can view their own, managers/admins can view all
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Customer" && feedback.CustomerId != userId)
            {
                return Forbid();
            }

            return Ok(ApiResponse<FeedbackDto>.SuccessResponse(feedback, "Feedback retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback");
            return StatusCode(500, ApiResponse<FeedbackDto>.ErrorResponse("An error occurred while retrieving feedback"));
        }
    }

    [HttpGet("my-feedbacks/list")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<List<FeedbackDto>>>> GetMyFeedbacks()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<FeedbackDto>>.ErrorResponse("User not authenticated"));
            }

            _logger.LogInformation("Customer {UserId} retrieving their feedbacks", userId);
            var feedbacks = await _feedbackService.GetCustomerFeedbacksAsync(userId);
            return Ok(ApiResponse<List<FeedbackDto>>.SuccessResponse(feedbacks, "Feedbacks retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer feedbacks");
            return StatusCode(500, ApiResponse<List<FeedbackDto>>.ErrorResponse("An error occurred while retrieving feedbacks"));
        }
    }

    [HttpGet("all")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<FeedbackListDto>>> GetAllFeedbacks([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("Manager/Admin retrieving all feedbacks - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            var feedbackList = await _feedbackService.GetAllFeedbacksAsync(page, pageSize);
            return Ok(ApiResponse<FeedbackListDto>.SuccessResponse(feedbackList, "Feedbacks retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all feedbacks");
            return StatusCode(500, ApiResponse<FeedbackListDto>.ErrorResponse("An error occurred while retrieving feedbacks"));
        }
    }
}
