using MealPreparationService.Business.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net; // Thêm thư viện này cho WebUtility
using System.Security.Cryptography;
using System.Text;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service for VNPay payment gateway integration (sandbox environment)
/// </summary>
public class VnPayService : IVnPayService
{
    private readonly ILogger<VnPayService> _logger;
    private readonly string _merchantId;
    private readonly string _hashSecret;
    private readonly string _paymentUrl;
    private readonly string _apiUrl;
    private readonly int _timeoutSeconds;
    private const string Version = "2.1.0";
    private const string Command = "pay";
    private const string CurrencyCode = "VND";
    private const string Locale = "vn";

    public VnPayService(
        IConfiguration configuration,
        ILogger<VnPayService> logger)
    {
        _logger = logger;
        
        _merchantId = configuration["VNPay:MerchantId"] 
            ?? throw new InvalidOperationException("VNPay MerchantId is not configured");
        
        // FIX 1: Thêm .Trim() để dọn dẹp khoảng trắng rác (nếu có) từ file config
        _hashSecret = (configuration["VNPay:HashSecret"] 
            ?? throw new InvalidOperationException("VNPay HashSecret is not configured")).Trim();
        
        _paymentUrl = configuration["VNPay:PaymentUrl"] 
            ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        
        _apiUrl = configuration["VNPay:ApiUrl"] 
            ?? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
        
        _timeoutSeconds = int.TryParse(configuration["SystemConfiguration:VnPayTimeoutSeconds"], out var timeout) 
            ? timeout : 15;
        
        _logger.LogInformation("VNPay service initialized with MerchantId: {MerchantId}, Timeout: {Timeout}s", 
            _merchantId, _timeoutSeconds);
    }

    public async Task<string> CreatePaymentUrlAsync(VnPayRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId)) throw new ArgumentException("OrderId is required", nameof(request));
        if (request.Amount <= 0) throw new ArgumentException("Amount must be greater than zero", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ReturnUrl)) throw new ArgumentException("ReturnUrl is required", nameof(request));

        try
        {
            var requestStartTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            
            _logger.LogInformation("VNPay: Received payment request - OrderId: {OrderId}, Amount: {Amount}", 
                request.OrderId, request.Amount);
            
            // FIX 2: Bắt lỗi IPv6 ở Localhost, chuyển thành IPv4 an toàn
            var ipAddress = string.IsNullOrWhiteSpace(request.IpAddress) || request.IpAddress.Contains("::1") 
                ? "127.0.0.1" : request.IpAddress;

            // FIX 3: Thêm StringComparer.Ordinal để ép C# sắp xếp từ khóa theo chuẩn ASCII
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                { "vnp_Version", Version },
                { "vnp_Command", Command },
                { "vnp_TmnCode", _merchantId },
                { "vnp_Amount", ((long)(request.Amount * 100)).ToString() }, // VNPay requires amount in smallest unit (xu = 1/100 dong)
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", CurrencyCode },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", Locale },
                { "vnp_OrderInfo", string.IsNullOrWhiteSpace(request.OrderInfo) ? $"Payment for Order {request.OrderId.Substring(0, 8).ToUpper()}" : request.OrderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", request.ReturnUrl },
                { "vnp_TxnRef", request.OrderId }
            };

            var queryString = BuildQueryString(parameters);
            var signature = GenerateSignature(queryString, _hashSecret);

            var paymentUrl = $"{_paymentUrl}?{queryString}&vnp_SecureHash={signature}";

            return await Task.FromResult(paymentUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay API: Error creating payment URL | Order: {OrderId}", request.OrderId);
            throw new InvalidOperationException("Failed to create payment URL", ex);
        }
    }

    public async Task<bool> ValidateSignatureAsync(Dictionary<string, string> parameters)
    {
        try
        {
            if (parameters == null || !parameters.Any() || !parameters.ContainsKey("vnp_SecureHash"))
            {
                return false;
            }

            var receivedSignature = parameters["vnp_SecureHash"];
            
            // FIX 3: Áp dụng StringComparer.Ordinal cho hàm nhận kết quả
            var validationParams = new SortedDictionary<string, string>(
                parameters.Where(p => p.Key != "vnp_SecureHash" && p.Key != "vnp_SecureHashType")
                          .ToDictionary(p => p.Key, p => p.Value),
                StringComparer.Ordinal
            );

            var signData = BuildQueryString(validationParams);
            var calculatedSignature = GenerateSignature(signData, _hashSecret);

            return await Task.FromResult(receivedSignature.Equals(calculatedSignature, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VNPay signature");
            return false;
        }
    }

    public async Task<VnPayCallbackDto> ProcessCallbackAsync(Dictionary<string, string> parameters)
    {
        // (Giữ nguyên logic của bạn)
        try
        {
            var isValidSignature = await ValidateSignatureAsync(parameters);
            if (!isValidSignature)
            {
                return new VnPayCallbackDto { Success = false, Message = "Invalid signature" };
            }

            var responseCode = parameters.GetValueOrDefault("vnp_ResponseCode", "");
            var orderId = parameters.GetValueOrDefault("vnp_TxnRef", "");
            var transactionId = parameters.GetValueOrDefault("vnp_TransactionNo", "");
            var amountStr = parameters.GetValueOrDefault("vnp_Amount", "0");
            var transactionDateStr = parameters.GetValueOrDefault("vnp_PayDate", "");

            var amount = long.TryParse(amountStr, out var amountValue) ? amountValue / 100m : 0; // VNPay returns amount in xu (1/100 dong)
            var transactionDate = DateTime.TryParseExact(transactionDateStr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ? parsedDate : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

            var success = responseCode == "00";
            return new VnPayCallbackDto
            {
                Success = success, OrderId = orderId, TransactionId = transactionId, Amount = amount,
                ResponseCode = responseCode, Message = GetResponseMessage(responseCode), TransactionDate = transactionDate
            };
        }
        catch (Exception)
        {
            return new VnPayCallbackDto { Success = false, Message = "Error processing payment callback" };
        }
    }

    public async Task<RefundResponseDto> RequestRefundAsync(RefundRequestDto request)
    {
        // ... Logic Request Refund ...
        var ipAddress = string.IsNullOrWhiteSpace(request.IpAddress) || request.IpAddress.Contains("::1") ? "127.0.0.1" : request.IpAddress;
        var requestId = Guid.NewGuid().ToString("N");
        
        // FIX 3: Thêm StringComparer.Ordinal
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Version", Version }, { "vnp_Command", "refund" }, { "vnp_TmnCode", _merchantId },
            { "vnp_TransactionType", "02" }, { "vnp_TxnRef", request.OrderId },
            { "vnp_Amount", ((long)(request.Amount * 100)).ToString() }, // VNPay requires amount in smallest unit (xu = 1/100 dong)
            { "vnp_OrderInfo", string.IsNullOrWhiteSpace(request.RefundReason) ? $"Refund {request.OrderId}" : request.RefundReason },
            { "vnp_TransactionNo", request.TransactionId },
            { "vnp_TransactionDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
            { "vnp_CreateBy", "System" }, { "vnp_IpAddr", ipAddress }, { "vnp_RequestId", requestId }
        };

        var signData = BuildQueryString(parameters);
        var signature = GenerateSignature(signData, _hashSecret);
        parameters.Add("vnp_SecureHash", signature);

        return await Task.FromResult(new RefundResponseDto { Success = true, RefundId = requestId, Message = "Simulated refund", ResponseCode = "00" });
    }

    #region Helper Methods

    private string BuildQueryString(SortedDictionary<string, string> parameters)
    {
        var queryString = new StringBuilder();
        foreach (var param in parameters)
        {
            if (!string.IsNullOrEmpty(param.Value))
            {
                if (queryString.Length > 0)
                {
                    queryString.Append('&');
                }
                // FIX 4: Đổi hàm encode sang WebUtility.UrlEncode để khớp với Java backend
                queryString.Append($"{param.Key}={WebUtility.UrlEncode(param.Value)}");
            }
        }
        return queryString.ToString();
    }

    private string GenerateSignature(string data, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    private string GetResponseMessage(string responseCode)
    {
        return responseCode switch
        {
            "00" => "Payment successful", "07" => "Transaction suspected of fraud",
            "09" => "Card not registered", "10" => "Incorrect auth 3 times",
            "11" => "Payment timeout", "12" => "Card locked", "24" => "Cancelled",
            "51" => "Insufficient balance", "65" => "Daily limit exceeded",
            _ => "Payment failed"
        };
    }
    #endregion
}
