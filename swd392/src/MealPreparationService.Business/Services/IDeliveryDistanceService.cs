using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for delivery distance validation and fee calculation
/// </summary>
public interface IDeliveryDistanceService
{
    /// <summary>
    /// Validates if delivery address is within maximum distance from service center
    /// </summary>
    /// <param name="deliveryAddress">Delivery address to validate</param>
    /// <returns>Validation result with distance information</returns>
    Task<DistanceValidationDto> ValidateDeliveryDistanceAsync(string deliveryAddress);
    
    /// <summary>
    /// Calculates delivery fee based on distance from service center
    /// </summary>
    /// <param name="deliveryAddress">Delivery address</param>
    /// <returns>Delivery fee calculation result</returns>
    Task<DeliveryFeeDto> CalculateDeliveryFeeAsync(string deliveryAddress);
    
    /// <summary>
    /// Gets the service center location
    /// </summary>
    /// <returns>Service center location</returns>
    LocationDto GetServiceCenterLocation();
}
