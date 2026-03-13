namespace MealPrepService.BusinessLogicLayer.DTOs;

public class DeliveryScheduleDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? DeliveryManId { get; set; }
    public string DeliveryManName { get; set; } = string.Empty;
    public DateTime DeliveryTime { get; set; }
    public string Address { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DriverContact { get; set; } = string.Empty;
    public OrderDto? Order { get; set; }

    // Helper properties for UI
    public bool IsOverdue => DeliveryTime < DateTime.Now &&
        Order?.Status != "customer_received" &&
        Order?.Status != "customer_reject" &&
        Order?.Status != "failed" &&
        Order?.Status != "cancelled";
    public string OrderStatus => Order?.Status ?? "unknown";
    public decimal TotalAmount => Order?.TotalAmount ?? 0;
    public string PaymentMethod => Order?.PaymentMethod ?? "unknown";
    public string CustomerName => Order?.CustomerName ?? "Unknown";
    public string CustomerContact => string.IsNullOrWhiteSpace(CustomerPhone)
        ? (Order?.CustomerContact ?? "N/A")
        : CustomerPhone;
    public decimal OrderTotal => Order?.TotalAmount ?? 0;
    public int ItemsCount => Order?.ItemsCount ?? 0;
    public string DeliveryNotes => Order?.DeliveryAddress ?? string.Empty;
}