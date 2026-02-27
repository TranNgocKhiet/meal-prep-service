namespace MealPrepService.BusinessLogicLayer.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? VnpayTransactionId { get; set; }
    public DateTime? PaymentConfirmedAt { get; set; }
    public Guid? PaymentConfirmedBy { get; set; }
    public List<OrderDetailDto> OrderDetails { get; set; } = new List<OrderDetailDto>();
    public DeliveryScheduleDto? DeliverySchedule { get; set; }

    // Additional properties for UI
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerContact { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public int ItemsCount => OrderDetails?.Sum(d => d.Quantity) ?? 0;
    public DateTime? DeliveryTime => DeliverySchedule?.DeliveryTime;
}