using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for PaymentGateway entity operations.
/// </summary>
public class PaymentGatewayRepository : Repository<PaymentGateway>, IPaymentGatewayRepository
{
    public PaymentGatewayRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a payment gateway by ID with related Status entity.
    /// </summary>
    public override async Task<PaymentGateway?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(pg => pg.Status)
            .FirstOrDefaultAsync(pg => pg.Id == id);
    }

    /// <summary>
    /// Gets a payment gateway by transaction number.
    /// </summary>
    public async Task<PaymentGateway?> GetByTransactionNoAsync(string transactionNo)
    {
        return await _dbSet
            .Include(pg => pg.Status)
            .FirstOrDefaultAsync(pg => pg.TransactionNo == transactionNo);
    }

    /// <summary>
    /// Gets all payment gateways with a specific status.
    /// </summary>
    public async Task<List<PaymentGateway>> GetByStatusAsync(int statusId)
    {
        return await _dbSet
            .Include(pg => pg.Status)
            .Where(pg => pg.StatusId == statusId)
            .OrderByDescending(pg => pg.PayDate)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all payment gateways within a date range.
    /// </summary>
    public async Task<List<PaymentGateway>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(pg => pg.Status)
            .Where(pg => pg.PayDate >= startDate && pg.PayDate <= endDate)
            .OrderBy(pg => pg.PayDate)
            .ToListAsync();
    }
}
