namespace MealPrepService.BusinessLogicLayer.DTOs;

public class DeliveryScheduleDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public DateTime DeliveryTime { get; set; }
    public string Address { get; set; } = string.Empty;
    public string DriverContact { get; set; } = string.Empty;
    public OrderDto? Order { get; set; }

    // Helper properties for UI
    public bool IsOverdue => DeliveryTime < DateTime.Now && Order?.Status != "delivered";
    public string OrderStatus => Order?.Status ?? "unknown";
    public decimal TotalAmount => Order?.TotalAmount ?? 0;
    public string PaymentMethod => Order?.PaymentMethod ?? "unknown";
    public string CustomerName => Order?.CustomerName ?? "Unknown";
    public string CustomerContact => Order?.CustomerContact ?? "N/A";
    public decimal OrderTotal => Order?.TotalAmount ?? 0;
    public int ItemsCount => Order?.OrderDetails?.Count ?? 0;
    public string DeliveryNotes => Order?.DeliveryAddress ?? string.Empty;
}