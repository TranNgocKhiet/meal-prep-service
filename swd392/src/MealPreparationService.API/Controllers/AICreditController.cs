using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MealPreparationService.API.Controllers;

[ApiController]
[Route("api/aicredit")]
public class AICreditController : ControllerBase
{
    private readonly IAICreditPackageService _packageService;
    private readonly IAICreditTransactionService _transactionService;
    private readonly ILogger<AICreditController> _logger;

    public AICreditController(
        IAICreditPackageService packageService,
        IAICreditTransactionService transactionService,
        ILogger<AICreditController> logger)
    {
        _packageService = packageService;
        _transactionService = transactionService;
        _logger = logger;
    }

    [HttpGet("packages")]
    public async Task<ActionResult<ApiResponse<List<AICreditPackageDto>>>> GetPackages()
    {
        try
        {
            var packages = await _packageService.GetAllAsync();
            return Ok(ApiResponse<List<AICreditPackageDto>>.SuccessResponse(packages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AI credit packages");
            return StatusCode(500, ApiResponse<List<AICreditPackageDto>>.ErrorResponse("Failed to get AI credit packages"));
        }
    }

    [HttpGet("transactions")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<AICreditTransactionDto>>>> GetMyTransactions()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<List<AICreditTransactionDto>>.ErrorResponse("User not authenticated"));
            }

            var transactions = await _transactionService.GetUserTransactionsAsync(userId);
            return Ok(ApiResponse<List<AICreditTransactionDto>>.SuccessResponse(transactions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user transactions");
            return StatusCode(500, ApiResponse<List<AICreditTransactionDto>>.ErrorResponse("Failed to get transactions"));
        }
    }

    [HttpPost("purchase")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AICreditPurchaseResponseDto>>> PurchaseCredits([FromBody] PurchaseAICreditDto dto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse<AICreditPurchaseResponseDto>.ErrorResponse("User not authenticated"));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _transactionService.PurchaseCreditsAsync(userId, dto, ipAddress);

            return Ok(ApiResponse<AICreditPurchaseResponseDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AICreditPurchaseResponseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purchasing AI credits");
            return StatusCode(500, ApiResponse<AICreditPurchaseResponseDto>.ErrorResponse("Failed to purchase AI credits"));
        }
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> PaymentCallback()
    {
        try
        {
            var vnpayData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            var transactionId = Request.Query["vnp_TxnRef"].ToString();

            var success = await _transactionService.ProcessPaymentCallbackAsync(transactionId, vnpayData);

            if (success)
            {
                return Ok(ApiResponse<object>.SuccessResponse(new { transactionId, success = true }, "Payment processed successfully"));
            }
            else
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Payment failed or was cancelled"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to process payment callback"));
        }
    }
}
