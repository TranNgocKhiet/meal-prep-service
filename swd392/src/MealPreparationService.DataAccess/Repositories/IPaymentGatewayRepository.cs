using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for PaymentGateway entity operations.
/// </summary>
public interface IPaymentGatewayRepository : IRepository<PaymentGateway>
{
    /// <summary>
    /// Gets a payment gateway by transaction number.
    /// </summary>
    Task<PaymentGateway?> GetByTransactionNoAsync(string transactionNo);
    
    /// <summary>
    /// Gets all payment gateways with a specific status.
    /// </summary>
    Task<List<PaymentGateway>> GetByStatusAsync(int statusId);
    
    /// <summary>
    /// Gets all payment gateways within a date range.
    /// </summary>
    Task<List<PaymentGateway>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
