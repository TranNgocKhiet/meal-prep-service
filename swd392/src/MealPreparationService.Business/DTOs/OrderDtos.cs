using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.Business.DTOs;

/// <summary>
/// DTO for creating a new order from cart
/// </summary>
public class CreateOrderDto
{
    [Required(ErrorMessage = "Payment method is required")]
    [RegularExpression("^(VNPay|Cash)$", ErrorMessage = "Payment method must be either 'VNPay' or 'Cash'")]
    public string PaymentMethod { get; set; } = string.Empty; // VNPay, Cash
    
    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 500 characters")]
    public string Address { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^(\+84|0)[0-9]{9,10}$", ErrorMessage = "Phone number must be a valid Vietnamese phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// DTO for order item in create request
/// </summary>
public class OrderItemDto
{
    [Required(ErrorMessage = "Ingredient ID is required")]
    public string IngredientId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.01, 10000, ErrorMessage = "Quantity must be between 0.01 and 10000")]
    public decimal Quantity { get; set; }
    
    [Required(ErrorMessage = "Unit is required")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "Unit must be between 1 and 20 characters")]
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// DTO for order response
/// </summary>
public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public List<OrderItemDetailDto> Items { get; set; } = new();
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? CancellationReason { get; set; }
    public DeliveryDto? Delivery { get; set; }
}

/// <summary>
/// DTO for order item detail in response
/// </summary>
public class OrderItemDetailDto
{
    public string IngredientId { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public string IngredientCategory { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// DTO for delivery information (basic)
/// </summary>
public class DeliveryDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryPersonnelId { get; set; }
    public string? DeliveryPersonnelName { get; set; }
    public LocationDto? CurrentLocation { get; set; }
    public TimeSpan? EstimatedDeliveryTime { get; set; }
}
