namespace MealPreparationService.Domain.Entities;

public class DeliverySchedule : BaseEntity
{
    public string DriverId { get; set; } = string.Empty;
    public Account Driver { get; set; } = null!;
    public string OrderId { get; set; } = string.Empty;
    public Order Order { get; set; } = null!;
    public DateTime DeliveryTime { get; set; }
    public string Address { get; set; } = string.Empty;
    public string DriverContact { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
