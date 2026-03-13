using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class VnpayService : IVnpayService
    {
        private readonly ILogger<VnpayService> _logger;
        private readonly string _vnpayUrl;
        private readonly string _vnpayTmnCode;
        private readonly string _vnpayHashSecret;
        private readonly string _vnpayReturnUrl;
        private readonly decimal _usdToVndExchangeRate;

        private const string Version = "2.1.0";
        private const string Command = "pay";
        private const string CurrencyCode = "VND";
        private const string Locale = "vn";
        private const decimal DefaultUsdToVndExchangeRate = 25000m;
        
        public VnpayService(IConfiguration configuration, ILogger<VnpayService> logger)
        {
            _logger = logger;

            _vnpayUrl =
                configuration["VnPay:Url"] ??
                configuration["VNPay:PaymentUrl"] ??
                "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            _vnpayTmnCode =
                configuration["VnPay:TmnCode"] ??
                configuration["VNPay:MerchantId"] ??
                throw new InvalidOperationException("VNPay TmnCode/MerchantId is not configured.");

            _vnpayHashSecret = (
                configuration["VnPay:HashSecret"] ??
                configuration["VNPay:HashSecret"] ??
                throw new InvalidOperationException("VNPay HashSecret is not configured.")
            ).Trim();

            _vnpayReturnUrl =
                configuration["VnPay:ReturnUrl"] ??
                throw new InvalidOperationException("VNPay ReturnUrl is not configured.");

            var exchangeRateConfig =
                configuration["VnPay:UsdToVndExchangeRate"] ??
                configuration["VNPay:UsdToVndExchangeRate"];

            if (!decimal.TryParse(exchangeRateConfig, out var parsedExchangeRate) || parsedExchangeRate <= 0)
            {
                parsedExchangeRate = DefaultUsdToVndExchangeRate;
            }

            _usdToVndExchangeRate = parsedExchangeRate;
        }
        
        public Task<VnpayPaymentUrlDto> CreatePaymentUrlAsync(Guid orderId, decimal amount, string orderInfo)
        {
            if (orderId == Guid.Empty)
            {
                throw new ArgumentException("Order ID is required.", nameof(orderId));
            }

            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
            }

            // System stores order totals in USD, VNPay requires VND.
            var amountInVnd = Math.Round(amount * _usdToVndExchangeRate, 0, MidpointRounding.AwayFromZero);
            if (amountInVnd <= 0)
            {
                throw new ArgumentException("Converted VNPay amount must be greater than zero.", nameof(amount));
            }

            var vnpayData = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                {"vnp_Version", Version},
                {"vnp_Command", Command},
                {"vnp_TmnCode", _vnpayTmnCode},
                {"vnp_Amount", ((long)(amountInVnd * 100)).ToString()}, // VNPay expects amount in VND x100
                {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                {"vnp_CurrCode", CurrencyCode},
                {"vnp_IpAddr", "127.0.0.1"},
                {"vnp_Locale", Locale},
                {"vnp_OrderInfo", orderInfo},
                {"vnp_OrderType", "other"},
                {"vnp_ReturnUrl", _vnpayReturnUrl},
                {"vnp_TxnRef", orderId.ToString()}
            };

            _logger.LogInformation("VNPay amount conversion for order {OrderId}: {UsdAmount} USD -> {VndAmount} VND at rate {Rate}",
                orderId, amount, amountInVnd, _usdToVndExchangeRate);
            
            // Create secure hash
            var hashData = BuildQueryString(vnpayData);
            var secureHash = CreateSecureHash(hashData, _vnpayHashSecret);
            vnpayData.Add("vnp_SecureHash", secureHash);
            
            // Build payment URL
            var queryString = BuildQueryString(vnpayData);
            var paymentUrl = $"{_vnpayUrl}?{queryString}";
            
            return Task.FromResult(new VnpayPaymentUrlDto
            {
                PaymentUrl = paymentUrl,
                TransactionId = orderId.ToString()
            });
        }
        
        public Task<VnpayCallbackResult> ProcessCallbackAsync(VnpayCallbackDto callbackDto)
        {
            try
            {
                // Validate callback
                if (!ValidateCallback(callbackDto))
                {
                    return Task.FromResult(new VnpayCallbackResult
                    {
                        IsSuccess = false,
                        Message = "Invalid callback signature"
                    });
                }
                
                // Parse order ID
                if (!Guid.TryParse(callbackDto.vnp_TxnRef, out var orderId))
                {
                    return Task.FromResult(new VnpayCallbackResult
                    {
                        IsSuccess = false,
                        Message = "Invalid order ID format"
                    });
                }
                
                return Task.FromResult(new VnpayCallbackResult
                {
                    IsSuccess = true,
                    OrderId = orderId,
                    TransactionId = callbackDto.vnp_TransactionNo,
                    ResponseCode = callbackDto.vnp_ResponseCode,
                    Message = GetResponseMessage(callbackDto.vnp_ResponseCode)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPAY callback");
                return Task.FromResult(new VnpayCallbackResult
                {
                    IsSuccess = false,
                    Message = "Error processing callback"
                });
            }
        }
        
        public bool ValidateCallback(VnpayCallbackDto callbackDto)
        {
            try
            {
                // Extract all parameters except secure hash
                var vnpayData = new SortedDictionary<string, string>(StringComparer.Ordinal);
                
                var properties = typeof(VnpayCallbackDto).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.Name == "vnp_SecureHash" || prop.Name == "vnp_SecureHashType")
                        continue;
                        
                    var value = prop.GetValue(callbackDto)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        vnpayData.Add(prop.Name, value);
                    }
                }
                
                // Create hash data
                var hashData = BuildQueryString(vnpayData);
                var computedHash = CreateSecureHash(hashData, _vnpayHashSecret);
                
                return computedHash.Equals(callbackDto.vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating VNPAY callback");
                return false;
            }
        }
        
        private string CreateSecureHash(string data, string secretKey)
        {
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        private static string BuildQueryString(SortedDictionary<string, string> parameters)
        {
            var queryString = new StringBuilder();

            foreach (var param in parameters)
            {
                if (string.IsNullOrWhiteSpace(param.Value))
                {
                    continue;
                }

                if (queryString.Length > 0)
                {
                    queryString.Append('&');
                }

                queryString.Append($"{param.Key}={WebUtility.UrlEncode(param.Value)}");
            }

            return queryString.ToString();
        }
        
        private string GetResponseMessage(string responseCode)
        {
            return responseCode switch
            {
                "00" => "Payment successful",
                "07" => "Transaction deducted successfully. Transaction is suspected of fraud (related to gray card/black card)",
                "09" => "Customer's card/account has not registered for InternetBanking service at the bank",
                "10" => "Customer entered incorrect card/account information more than 3 times",
                "11" => "Payment deadline has expired. Please retry the transaction",
                "12" => "Customer's card/account is locked",
                "13" => "Customer entered incorrect transaction authentication password (OTP)",
                "24" => "Customer canceled the transaction",
                "51" => "Customer's account has insufficient balance to make the transaction",
                "65" => "Customer's account has exceeded the daily transaction limit",
                _ => "Transaction failed"
            };
        }
    }
}