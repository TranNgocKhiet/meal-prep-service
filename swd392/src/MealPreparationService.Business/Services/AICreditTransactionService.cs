using MealPreparationService.Domain.Services;
using MealPreparationService.Business.DTOs;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class AICreditTransactionService : IAICreditTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVnPayService _vnPayService;
    private readonly ILogger<AICreditTransactionService> _logger;
    private readonly IDateTimeService _dateTimeService;

    public AICreditTransactionService(
        IUnitOfWork unitOfWork,
        IVnPayService vnPayService,
        ILogger<AICreditTransactionService> logger,
        IDateTimeService dateTimeService)
    {
        _unitOfWork = unitOfWork;
        _vnPayService = vnPayService;
        _logger = logger;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<AICreditTransactionDto>> GetUserTransactionsAsync(string userId)
    {
        var transactions = await _unitOfWork.AICreditTransactions.GetAllQueryable()
            .Include(t => t.AIcreditPackage)
            .Include(t => t.PaymentGateway)
            .Where(t => t.AccountId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(MapToDto).ToList();
    }

    public async Task<AICreditPurchaseResponseDto> PurchaseCreditsAsync(string userId, PurchaseAICreditDto dto, string ipAddress)
    {
        _logger.LogInformation("User {UserId} purchasing AI credit package {PackageId}", userId, dto.AIcreditPackageId);

        // Get the package
        var package = await _unitOfWork.AICreditPackages.GetByIdAsync(dto.AIcreditPackageId);
        if (package == null)
        {
            throw new KeyNotFoundException($"AI Credit Package {dto.AIcreditPackageId} not found");
        }

        // Create payment gateway record
        var paymentGateway = new PaymentGateway
        {
            Id = Guid.NewGuid().ToString(),
            StatusId = 1, // Pending
            TransactionNo = "",
            BankCode = "",
            ResponseCode = "",
            PayDate = _dateTimeService.Now
        };

        await _unitOfWork.PaymentGateways.AddAsync(paymentGateway);

        // Create transaction record
        var transaction = new AIcreditTransaction
        {
            Id = Guid.NewGuid().ToString(),
            AccountId = userId,
            AIcreditPackageId = dto.AIcreditPackageId,
            PaymentGatewayId = paymentGateway.Id,
            CreatedAt = _dateTimeService.Now
        };

        await _unitOfWork.AICreditTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        // Create VNPay payment URL
        var vnpayRequest = new VnPayRequestDto
        {
            OrderId = transaction.Id,
            Amount = package.Price,
            OrderInfo = $"Purchase {package.PackageName} - {package.CreditAmount} credits",
            ReturnUrl = $"{GetBaseUrl()}/ai-credits/callback",
            IpAddress = ipAddress
        };

        var paymentUrl = await _vnPayService.CreatePaymentUrlAsync(vnpayRequest);

        return new AICreditPurchaseResponseDto
        {
            TransactionId = transaction.Id,
            PaymentUrl = paymentUrl,
            RequiresPayment = true
        };
    }

    public async Task<bool> ProcessPaymentCallbackAsync(string transactionId, Dictionary<string, string> vnpayData)
    {
        _logger.LogInformation("Processing payment callback for transaction {TransactionId}", transactionId);

        var transaction = await _unitOfWork.AICreditTransactions.GetAllQueryable()
            .Include(t => t.PaymentGateway)
            .Include(t => t.AIcreditPackage)
            .Include(t => t.Account)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
        {
            _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
            return false;
        }

        // Use VnPayService to process the callback properly
        var callbackResult = await _vnPayService.ProcessCallbackAsync(vnpayData);
        
        if (!callbackResult.Success)
        {
            _logger.LogWarning("VNPay payment failed for transaction {TransactionId}: {Message}", 
                transactionId, callbackResult.Message);
            
            // Update payment gateway as failed
            transaction.PaymentGateway.StatusId = 5; // Cancelled/Failed
            transaction.PaymentGateway.ResponseCode = callbackResult.ResponseCode ?? "";
            await _unitOfWork.SaveChangesAsync();
            
            return false;
        }

        // Payment successful
        transaction.PaymentGateway.StatusId = 3; // Confirmed/Paid
        transaction.PaymentGateway.TransactionNo = callbackResult.TransactionId ?? "";
        transaction.PaymentGateway.BankCode = ""; // BankCode not available in callback
        transaction.PaymentGateway.ResponseCode = callbackResult.ResponseCode ?? "00";
        transaction.PaymentGateway.PayDate = _dateTimeService.Now;

        // Add credits to user account
        transaction.Account.CurrentCredits += transaction.AIcreditPackage.CreditAmount;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Successfully processed payment for transaction {TransactionId}. Added {Credits} credits to user {UserId}",
            transactionId, transaction.AIcreditPackage.CreditAmount, transaction.AccountId);

        return true;
    }

    private string GetBaseUrl()
    {
        // This should be configured in appsettings
        return "http://localhost:5173";
    }

    private AICreditTransactionDto MapToDto(AIcreditTransaction transaction)
    {
        return new AICreditTransactionDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            AIcreditPackageId = transaction.AIcreditPackageId,
            PackageName = transaction.AIcreditPackage.PackageName,
            CreditAmount = transaction.AIcreditPackage.CreditAmount,
            Price = transaction.AIcreditPackage.Price,
            PaymentGatewayId = transaction.PaymentGatewayId,
            PaymentMethod = "VNPay",
            CreatedAt = transaction.CreatedAt
        };
    }
}


