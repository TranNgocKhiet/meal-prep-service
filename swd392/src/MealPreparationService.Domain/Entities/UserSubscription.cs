namespace MealPreparationService.Domain.Entities;

public class UserSubscription : BaseEntity
{
    public string AccountId { get; set; } = string.Empty;
    public Account Account { get; set; } = null!;
    public string SubscriptionPackageId { get; set; } = string.Empty;
    public SubscriptionPackage SubscriptionPackage { get; set; } = null!;
    public string PaymentGatewayId { get; set; } = string.Empty;
    public PaymentGateway PaymentGateway { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}



