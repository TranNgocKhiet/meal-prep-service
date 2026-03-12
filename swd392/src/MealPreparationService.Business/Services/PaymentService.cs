using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class PaymentService : IPaymentService
{
    private readonly IVnPayService _vnPayService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IVnPayService vnPayService,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger)
    {
        _vnPayService = vnPayService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentUrlDto> CreatePaymentUrlAsync(string orderId, decimal amount, string returnUrl, string ipAddress)
    {
        try
        {
            var request = new VnPayRequestDto
            {
                OrderId = orderId,
                Amount = amount,
                OrderInfo = $"Payment for Order #{orderId.Substring(0, 8).ToUpper()}",
                ReturnUrl = returnUrl,
                IpAddress = ipAddress
            };

            var paymentUrl = await _vnPayService.CreatePaymentUrlAsync(request);

            return new PaymentUrlDto
            {
                PaymentUrl = paymentUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment URL for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<PaymentResultDto> ProcessPaymentCallbackAsync(Dictionary<string, string> parameters)
    {
        try
        {
            _logger.LogInformation("Processing VNPay payment callback");

            // Process VNPay callback
            var callbackResult = await _vnPayService.ProcessCallbackAsync(parameters);

            if (!callbackResult.Success)
            {
                _logger.LogWarning("VNPay payment failed: {Message}", callbackResult.Message);
                return new PaymentResultDto
                {
                    Success = false,
                    OrderId = callbackResult.OrderId,
                    Message = callbackResult.Message
                };
            }

            // Get the order
            var order = await _unitOfWork.Orders.GetByIdAsync(callbackResult.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", callbackResult.OrderId);
                return new PaymentResultDto
                {
                    Success = false,
                    OrderId = callbackResult.OrderId,
                    Message = "Order not found"
                };
            }

            // Create PaymentGateway record
            var paymentGateway = new Domain.Entities.PaymentGateway
            {
                Id = Guid.NewGuid().ToString(),
                StatusId = 3, // Confirmed (OrderConfirmed)
                TransactionNo = callbackResult.TransactionId,
                BankCode = parameters.GetValueOrDefault("vnp_BankCode", ""),
                ResponseCode = callbackResult.ResponseCode,
                PayDate = callbackResult.TransactionDate
            };

            await _unitOfWork.PaymentGateways.AddAsync(paymentGateway);

            // Update order with payment gateway and set status to Confirmed (3 = OrderConfirmed)
            order.PaymentGatewayId = paymentGateway.Id;
            order.StatusId = 3; // OrderConfirmed
            order.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} confirmed via VNPay payment. PaymentGateway {PaymentGatewayId} created.", 
                callbackResult.OrderId, paymentGateway.Id);

            return new PaymentResultDto
            {
                Success = true,
                OrderId = callbackResult.OrderId,
                TransactionId = callbackResult.TransactionId,
                Amount = callbackResult.Amount,
                Message = "Payment successful"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment callback");
            return new PaymentResultDto
            {
                Success = false,
                Message = "An error occurred while processing payment"
            };
        }
    }

    public async Task<RefundResultDto> ProcessRefundAsync(string orderId, string transactionId, decimal amount, string reason, string ipAddress)
    {
        try
        {
            var request = new RefundRequestDto
            {
                OrderId = orderId,
                TransactionId = transactionId,
                Amount = amount,
                RefundReason = reason,
                IpAddress = ipAddress
            };

            var refundResponse = await _vnPayService.RequestRefundAsync(request);

            return new RefundResultDto
            {
                Success = refundResponse.Success,
                RefundId = refundResponse.RefundId,
                Message = refundResponse.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for order {OrderId}", orderId);
            return new RefundResultDto
            {
                Success = false,
                Message = "An error occurred while processing refund"
            };
        }
    }

    public async Task<bool> ValidatePaymentSignatureAsync(Dictionary<string, string> parameters)
    {
        return await _vnPayService.ValidateSignatureAsync(parameters);
    }
}
