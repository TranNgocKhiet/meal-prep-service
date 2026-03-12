using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for payment processing
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a payment URL for an order
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="amount">Payment amount</param>
    /// <param name="returnUrl">URL to return after payment</param>
    /// <param name="ipAddress">Customer IP address</param>
    /// <returns>Payment URL DTO</returns>
    Task<PaymentUrlDto> CreatePaymentUrlAsync(string orderId, decimal amount, string returnUrl, string ipAddress);
    
    /// <summary>
    /// Processes payment callback and updates order status
    /// </summary>
    /// <param name="parameters">Callback parameters from payment gateway</param>
    /// <returns>Payment result DTO</returns>
    Task<PaymentResultDto> ProcessPaymentCallbackAsync(Dictionary<string, string> parameters);
    
    /// <summary>
    /// Processes refund for a cancelled order
    /// </summary>
    /// <param name="orderId">Order ID</param>
    /// <param name="transactionId">Payment transaction ID</param>
    /// <param name="amount">Refund amount</param>
    /// <param name="reason">Refund reason</param>
    /// <param name="ipAddress">Staff IP address</param>
    /// <returns>Refund result DTO</returns>
    Task<RefundResultDto> ProcessRefundAsync(string orderId, string transactionId, decimal amount, string reason, string ipAddress);
    
    /// <summary>
    /// Validates payment signature
    /// </summary>
    /// <param name="parameters">Payment callback parameters</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> ValidatePaymentSignatureAsync(Dictionary<string, string> parameters);
}
