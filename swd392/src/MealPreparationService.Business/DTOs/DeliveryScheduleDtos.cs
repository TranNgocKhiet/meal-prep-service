using System.ComponentModel.DataAnnotations;

namespace MealPreparationService.Business.DTOs;

/// <summary>
/// DTO for creating a delivery schedule/shift
/// </summary>
public class CreateDeliveryScheduleDto
{
    [Required(ErrorMessage = "Driver ID is required")]
    public string DriverId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Order ID is required")]
    public string OrderId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Delivery time is required")]
    public DateTime DeliveryTime { get; set; }
    
    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 500 characters")]
    public string Address { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Driver contact is required")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Driver contact must be between 10 and 20 characters")]
    public string DriverContact { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating a delivery schedule
/// </summary>
public class UpdateDeliveryScheduleDto
{
    public string? DriverId { get; set; }
    public DateTime? DeliveryTime { get; set; }
    
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 500 characters")]
    public string? Address { get; set; }
    
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Driver contact must be between 10 and 20 characters")]
    public string? DriverContact { get; set; }
}

/// <summary>
/// DTO for delivery schedule response
/// </summary>
public class DeliveryScheduleDto
{
    public string Id { get; set; } = string.Empty;
    public string DriverId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverEmail { get; set; } = string.Empty;
    public string DriverContact { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal OrderTotal { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime DeliveryTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for getting available drivers
/// </summary>
public class DriverDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
