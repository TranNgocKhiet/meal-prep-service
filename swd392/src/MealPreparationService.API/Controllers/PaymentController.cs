using MealPreparationService.API.Models;
using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealPreparationService.API.Controllers;

/// <summary>
/// Controller for payment processing
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService paymentService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a payment URL for an order
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="returnUrl">URL to return after payment</param>
    /// <returns>Payment URL</returns>
    [HttpPost("create-url")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentUrlRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Order ID is required"));
            }

            if (string.IsNullOrWhiteSpace(request.ReturnUrl))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Return URL is required"));
            }

            // Get client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _paymentService.CreatePaymentUrlAsync(
                request.OrderId,
                request.Amount,
                request.ReturnUrl,
                ipAddress);

            return Ok(ApiResponse<object>.SuccessResponse(result, "Payment URL created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating payment URL");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment URL");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to create payment URL"));
        }
    }

    /// <summary>
    /// Handles payment callback from VNPay (webhook endpoint)
    /// </summary>
    /// <returns>Payment result</returns>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback()
    {
        try
        {
            // Extract all query parameters
            var parameters = Request.Query.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString()
            );

            _logger.LogInformation("Received payment callback with {Count} parameters", parameters.Count);

            var result = await _paymentService.ProcessPaymentCallbackAsync(parameters);

            if (result.Success)
            {
                return Ok(ApiResponse<PaymentResultDto>.SuccessResponse(result, "Payment processed successfully"));
            }
            else
            {
                return BadRequest(ApiResponse<PaymentResultDto>.ErrorResponse(result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to process payment callback"));
        }
    }

    /// <summary>
    /// Handles payment webhook notification from VNPay (asynchronous notification)
    /// </summary>
    /// <returns>Webhook acknowledgment</returns>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentWebhook()
    {
        try
        {
            // Extract form parameters
            var parameters = Request.Form.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToString()
            );

            _logger.LogInformation("Received payment webhook with {Count} parameters", parameters.Count);

            // Validate signature
            var isValid = await _paymentService.ValidatePaymentSignatureAsync(parameters);
            if (!isValid)
            {
                _logger.LogWarning("Invalid payment webhook signature");
                return BadRequest(new { RspCode = "97", Message = "Invalid signature" });
            }

            // Process callback
            var result = await _paymentService.ProcessPaymentCallbackAsync(parameters);

            // Return VNPay expected response format
            if (result.Success)
            {
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            else
            {
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment webhook");
            return Ok(new { RspCode = "99", Message = "System error" });
        }
    }

    /// <summary>
    /// Processes a refund for a cancelled order (Staff only)
    /// </summary>
    /// <param name="request">Refund request</param>
    /// <returns>Refund result</returns>
    [HttpPost("refund")]
    [Authorize(Policy = "StaffOnly")]
    public async Task<IActionResult> ProcessRefund([FromBody] ProcessRefundRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Order ID is required"));
            }

            if (string.IsNullOrWhiteSpace(request.TransactionId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Transaction ID is required"));
            }

            if (request.Amount <= 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Amount must be greater than zero"));
            }

            // Get staff IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            var result = await _paymentService.ProcessRefundAsync(
                request.OrderId,
                request.TransactionId,
                request.Amount,
                request.Reason,
                ipAddress);

            if (result.Success)
            {
                return Ok(ApiResponse<RefundResultDto>.SuccessResponse(result, "Refund processed successfully"));
            }
            else
            {
                return BadRequest(ApiResponse<RefundResultDto>.ErrorResponse(result.Message));
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation processing refund");
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to process refund"));
        }
    }
}

/// <summary>
/// Request model for creating payment URL
/// </summary>
public class CreatePaymentUrlRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request model for processing refund
/// </summary>
public class ProcessRefundRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}
