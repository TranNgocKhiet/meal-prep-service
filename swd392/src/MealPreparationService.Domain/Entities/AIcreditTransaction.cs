namespace MealPreparationService.Domain.Entities;

public class AIcreditTransaction : BaseEntity
{
    public string AccountId { get; set; } = string.Empty;
    public Account Account { get; set; } = null!;
    public string AIcreditPackageId { get; set; } = string.Empty;
    public AIcreditPackage AIcreditPackage { get; set; } = null!;
    public string PaymentGatewayId { get; set; } = string.Empty;
    public PaymentGateway PaymentGateway { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
