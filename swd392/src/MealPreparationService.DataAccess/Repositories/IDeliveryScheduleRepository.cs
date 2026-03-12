using MealPreparationService.Domain.Entities;

namespace MealPreparationService.DataAccess.Repositories;

/// <summary>
/// Repository interface for DeliverySchedule entity operations.
/// </summary>
public interface IDeliveryScheduleRepository : IRepository<DeliverySchedule>
{
    /// <summary>
    /// Gets all delivery schedules for a specific driver.
    /// </summary>
    Task<List<DeliverySchedule>> GetByDriverIdAsync(string driverId);
    
    /// <summary>
    /// Gets all delivery schedules within a date range.
    /// </summary>
    Task<List<DeliverySchedule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    
    /// <summary>
    /// Gets upcoming deliveries for a specific driver (future deliveries ordered by delivery time).
    /// </summary>
    Task<List<DeliverySchedule>> GetUpcomingDeliveriesAsync(string driverId);
}
