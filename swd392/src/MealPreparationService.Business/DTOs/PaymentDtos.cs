namespace MealPreparationService.Business.DTOs;

/// <summary>
/// DTO for payment URL response
/// </summary>
public class PaymentUrlDto
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}

/// <summary>
/// DTO for payment result from callback
/// </summary>
public class PaymentResultDto
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for refund result
/// </summary>
public class RefundResultDto
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for VNPay payment request
/// </summary>
public class VnPayRequestDto
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OrderInfo { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

/// <summary>
/// DTO for VNPay callback data
/// </summary>
public class VnPayCallbackDto
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ResponseCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
}

/// <summary>
/// DTO for refund request
/// </summary>
public class RefundRequestDto
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string RefundReason { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

/// <summary>
/// DTO for refund response from VNPay
/// </summary>
public class RefundResponseDto
{
    public bool Success { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
}
