namespace MealPreparationService.Business.DTOs;

/// <summary>
/// DTO for assigning delivery to personnel
/// </summary>
public class AssignDeliveryDto
{
    public string OrderId { get; set; } = string.Empty;
    public string DeliveryPersonnelId { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating delivery status
/// </summary>
public class UpdateDeliveryStatusDto
{
    public string Status { get; set; } = string.Empty; // Assigned, PickedUp, InTransit, Delivered, Failed
}

/// <summary>
/// DTO for updating delivery location
/// </summary>
public class UpdateLocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>
/// DTO for delivery confirmation
/// </summary>
public class DeliveryConfirmationDto
{
    public string ConfirmationType { get; set; } = string.Empty; // Signature, Photo
    public string ConfirmationData { get; set; } = string.Empty; // Base64 encoded
}

/// <summary>
/// DTO for marking delivery as failed
/// </summary>
public class DeliveryFailureDto
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Extended delivery DTO with full details
/// </summary>
public class DeliveryDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string DeliveryPersonnelId { get; set; } = string.Empty;
    public string DeliveryPersonnelName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public LocationDto? CurrentLocation { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public TimeSpan? EstimatedDeliveryTime { get; set; }
    public string? DeliveryConfirmationType { get; set; }
    public string? FailureReason { get; set; }
    public List<LocationDto> LocationHistory { get; set; } = new();
}
