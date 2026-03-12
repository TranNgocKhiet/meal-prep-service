using MealPreparationService.DataAccess.Data;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository implementation for DeliverySchedule entity operations.
/// </summary>
public class DeliveryScheduleRepository : Repository<DeliverySchedule>, IDeliveryScheduleRepository
{
    public DeliveryScheduleRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a delivery schedule by ID with related Driver (Account) and Order entities.
    /// </summary>
    public override async Task<DeliverySchedule?> GetByIdAsync(string id)
    {
        return await _dbSet
            .Include(ds => ds.Driver)
            .Include(ds => ds.Order)
            .FirstOrDefaultAsync(ds => ds.Id == id);
    }

    /// <summary>
    /// Gets all delivery schedules for a specific driver.
    /// </summary>
    public async Task<List<DeliverySchedule>> GetByDriverIdAsync(string driverId)
    {
        return await _dbSet
            .Include(ds => ds.Driver)
            .Include(ds => ds.Order)
            .Where(ds => ds.DriverId == driverId)
            .OrderBy(ds => ds.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets all delivery schedules within a date range.
    /// </summary>
    public async Task<List<DeliverySchedule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(ds => ds.Driver)
            .Include(ds => ds.Order)
            .Where(ds => ds.CreatedAt >= startDate && ds.CreatedAt <= endDate)
            .OrderBy(ds => ds.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Gets upcoming deliveries for a specific driver (future deliveries ordered by created time).
    /// </summary>
    public async Task<List<DeliverySchedule>> GetUpcomingDeliveriesAsync(string driverId)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(ds => ds.Driver)
            .Include(ds => ds.Order)
            .Where(ds => ds.DriverId == driverId && ds.CreatedAt > now)
            .OrderBy(ds => ds.CreatedAt)
            .ToListAsync();
    }
}
