namespace MealPreparationService.Domain.Entities;

public class RevenueReport : BaseEntity
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalSubscriptionRev { get; set; }
    public decimal TotalOrderRev { get; set; }
    public decimal TotalAiCreditRev { get; set; }
    public int TotalOrdersCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
