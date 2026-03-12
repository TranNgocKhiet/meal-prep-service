using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for delivery tracking and management operations
/// </summary>
public interface IDeliveryService
{
    /// <summary>
    /// Assigns a delivery to delivery personnel
    /// </summary>
    Task<DeliveryDetailDto> AssignDeliveryAsync(string orderId, string deliveryPersonnelId);

    /// <summary>
    /// Updates delivery status (PickedUp, InTransit, Delivered, Failed)
    /// </summary>
    Task<DeliveryDetailDto> UpdateDeliveryStatusAsync(string deliveryId, string status);

    /// <summary>
    /// Updates GPS location for a delivery (30-second interval)
    /// </summary>
    Task UpdateLocationAsync(string deliveryId, LocationDto location);

    /// <summary>
    /// Gets current location for a delivery
    /// </summary>
    Task<LocationDto?> GetCurrentLocationAsync(string deliveryId);

    /// <summary>
    /// Gets location history for a delivery
    /// </summary>
    Task<List<LocationDto>> GetLocationHistoryAsync(string deliveryId);

    /// <summary>
    /// Gets all assigned deliveries for a delivery personnel
    /// </summary>
    Task<List<DeliveryDetailDto>> GetAssignedDeliveriesAsync(string deliveryPersonnelId);

    /// <summary>
    /// Gets delivery details by ID
    /// </summary>
    Task<DeliveryDetailDto> GetDeliveryByIdAsync(string deliveryId);

    /// <summary>
    /// Gets delivery details by order ID
    /// </summary>
    Task<DeliveryDetailDto?> GetDeliveryByOrderIdAsync(string orderId);

    /// <summary>
    /// Confirms delivery with signature or photo
    /// </summary>
    Task<DeliveryDetailDto> ConfirmDeliveryAsync(string deliveryId, DeliveryConfirmationDto confirmation);

    /// <summary>
    /// Marks delivery as failed with reason
    /// </summary>
    Task<DeliveryDetailDto> MarkDeliveryFailedAsync(string deliveryId, string reason);

    /// <summary>
    /// Calculates estimated delivery time based on current location and destination
    /// </summary>
    Task<TimeSpan> CalculateEstimatedTimeAsync(string deliveryId);
}
