using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

public class RevenueReportRepository : Repository<RevenueReport>, IRevenueReportRepository
{
    public RevenueReportRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RevenueReport?> GetByMonthYearAsync(int month, int year)
    {
        return await _dbSet
            .FirstOrDefaultAsync(rr => rr.Month == month && rr.Year == year);
    }

    public async Task<List<RevenueReport>> GetByYearAsync(int year)
    {
        return await _dbSet
            .Where(rr => rr.Year == year)
            .OrderBy(rr => rr.Month)
            .ToListAsync();
    }

    public async Task<List<RevenueReport>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        int startYear = startDate.Year;
        int startMonth = startDate.Month;
        int endYear = endDate.Year;
        int endMonth = endDate.Month;

        return await _dbSet
            .Where(rr => 
                (rr.Year > startYear || (rr.Year == startYear && rr.Month >= startMonth)) &&
                (rr.Year < endYear || (rr.Year == endYear && rr.Month <= endMonth)))
            .OrderBy(rr => rr.Year)
            .ThenBy(rr => rr.Month)
            .ToListAsync();
    }
}
