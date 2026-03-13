namespace MealPreparationService.Domain.Entities;

public class SubscriptionPackage : BaseEntity
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CreditAmount { get; set; }
    public int DurationDays { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}



