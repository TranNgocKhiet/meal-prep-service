using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

public interface IRevenueReportRepository : IRepository<RevenueReport>
{
    Task<RevenueReport?> GetByMonthYearAsync(int month, int year);
    Task<List<RevenueReport>> GetByYearAsync(int year);
    Task<List<RevenueReport>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
