using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for VNPay payment gateway integration
/// </summary>
public interface IVnPayService
{
    /// <summary>
    /// Creates a payment URL for VNPay sandbox gateway
    /// </summary>
    /// <param name="request">Payment request details</param>
    /// <returns>Payment URL for redirection</returns>
    Task<string> CreatePaymentUrlAsync(VnPayRequestDto request);
    
    /// <summary>
    /// Validates payment signature from VNPay callback
    /// </summary>
    /// <param name="parameters">Callback parameters from VNPay</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> ValidateSignatureAsync(Dictionary<string, string> parameters);
    
    /// <summary>
    /// Processes payment callback from VNPay
    /// </summary>
    /// <param name="parameters">Callback parameters from VNPay</param>
    /// <returns>Processed callback data</returns>
    Task<VnPayCallbackDto> ProcessCallbackAsync(Dictionary<string, string> parameters);
    
    /// <summary>
    /// Requests a refund for a paid transaction
    /// </summary>
    /// <param name="request">Refund request details</param>
    /// <returns>Refund response</returns>
    Task<RefundResponseDto> RequestRefundAsync(RefundRequestDto request);
}
