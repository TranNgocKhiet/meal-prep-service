namespace MealPreparationService.Domain.Entities;

public class Order : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public Account Customer { get; set; } = null!;
    public string? PaymentGatewayId { get; set; }
    public PaymentGateway? PaymentGateway { get; set; }
    public string? OrderConfirmedBy { get; set; }
    public Account? ConfirmedByAccount { get; set; }
    public int StatusId { get; set; }
    public Status Status { get; set; } = null!;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<DeliverySchedule> DeliverySchedules { get; set; } = new List<DeliverySchedule>();
}
