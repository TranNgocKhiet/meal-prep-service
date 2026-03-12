namespace MealPreparationService.Domain.Entities;

public class PaymentGateway : BaseEntity
{
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
    public string TransactionNo { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public DateTime PayDate { get; set; }
    
    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<AIcreditTransaction> AIcreditTransactions { get; set; } = new List<AIcreditTransaction>();
}
