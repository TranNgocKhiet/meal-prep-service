using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

public interface IAICreditTransactionService
{
    Task<List<AICreditTransactionDto>> GetUserTransactionsAsync(string userId);
    Task<AICreditPurchaseResponseDto> PurchaseCreditsAsync(string userId, PurchaseAICreditDto dto, string ipAddress);
    Task<bool> ProcessPaymentCallbackAsync(string transactionId, Dictionary<string, string> vnpayData);
}
