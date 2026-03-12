using MealPreparationService.Business.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service for delivery distance validation and fee calculation
/// </summary>
public class DeliveryDistanceService : IDeliveryDistanceService
{
    private readonly IGoogleMapsService _googleMapsService;
    private readonly ILogger<DeliveryDistanceService> _logger;
    private readonly double _maxDeliveryDistanceKm;
    private readonly LocationDto _serviceCenterLocation;
    private readonly decimal _baseDeliveryFee;
    private readonly decimal _feePerKm;

    public DeliveryDistanceService(
        IGoogleMapsService googleMapsService,
        IConfiguration configuration,
        ILogger<DeliveryDistanceService> logger)
    {
        _googleMapsService = googleMapsService;
        _logger = logger;
        
        // Load configuration
        _maxDeliveryDistanceKm = double.TryParse(
            configuration["SystemConfiguration:MaxDeliveryDistanceKm"], 
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var maxDist) ? maxDist : 10.0;
        
        var serviceCenterLat = double.TryParse(
            configuration["GoogleMaps:ServiceCenterLatitude"], 
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var lat) ? lat : 0.0;
        
        var serviceCenterLng = double.TryParse(
            configuration["GoogleMaps:ServiceCenterLongitude"], 
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var lng) ? lng : 0.0;
        
        if (serviceCenterLat == 0.0 || serviceCenterLng == 0.0)
        {
            throw new InvalidOperationException(
                "Service center coordinates are not configured. " +
                "Please set GoogleMaps:ServiceCenterLatitude and GoogleMaps:ServiceCenterLongitude in configuration.");
        }
        
        _serviceCenterLocation = new LocationDto
        {
            Latitude = serviceCenterLat,
            Longitude = serviceCenterLng,
            Address = "Service Center",
            Timestamp = DateTime.UtcNow
        };
        
        // Load delivery fee configuration
        _baseDeliveryFee = decimal.TryParse(
            configuration["DeliveryFee:BaseFee"], 
            out var baseFee) ? baseFee : 20000m; // Default 20,000 VND
        
        _feePerKm = decimal.TryParse(
            configuration["DeliveryFee:FeePerKm"], 
            out var feePerKm) ? feePerKm : 5000m; // Default 5,000 VND per km
        
        _logger.LogInformation(
            "Delivery distance service initialized. Service center: ({Lat},{Lng}), Max distance: {MaxDist} km, " +
            "Base fee: {BaseFee} VND, Fee per km: {FeePerKm} VND",
            serviceCenterLat, serviceCenterLng, _maxDeliveryDistanceKm, _baseDeliveryFee, _feePerKm);
    }

    /// <summary>
    /// Validates if delivery address is within maximum distance from service center
    /// </summary>
    public async Task<DistanceValidationDto> ValidateDeliveryDistanceAsync(string deliveryAddress)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                return new DistanceValidationDto
                {
                    IsValid = false,
                    DistanceKm = 0,
                    MaxDistanceKm = _maxDeliveryDistanceKm,
                    Message = "Delivery address cannot be empty"
                };
            }

            _logger.LogInformation("Validating delivery distance for address: {Address}", deliveryAddress);

            // Geocode the delivery address
            var deliveryLocation = await _googleMapsService.GeocodeAddressAsync(deliveryAddress);

            // Calculate distance from service center
            var distance = await _googleMapsService.CalculateDistanceAsync(
                _serviceCenterLocation.Latitude,
                _serviceCenterLocation.Longitude,
                deliveryLocation.Latitude,
                deliveryLocation.Longitude);

            var isValid = distance <= _maxDeliveryDistanceKm;

            var message = isValid
                ? $"Delivery address is within service area ({distance:F2} km from service center)"
                : $"Delivery address exceeds maximum distance. Distance: {distance:F2} km, Maximum: {_maxDeliveryDistanceKm} km";

            _logger.LogInformation(
                "Distance validation result: {IsValid}, Distance: {Distance} km, Max: {MaxDistance} km",
                isValid, distance, _maxDeliveryDistanceKm);

            return new DistanceValidationDto
            {
                IsValid = isValid,
                DistanceKm = distance,
                MaxDistanceKm = _maxDeliveryDistanceKm,
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating delivery distance for address: {Address}", deliveryAddress);
            throw new InvalidOperationException("Failed to validate delivery distance. Please check the address and try again.", ex);
        }
    }

    /// <summary>
    /// Calculates delivery fee based on distance from service center
    /// </summary>
    public async Task<DeliveryFeeDto> CalculateDeliveryFeeAsync(string deliveryAddress)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                throw new ArgumentException("Delivery address cannot be empty", nameof(deliveryAddress));
            }

            _logger.LogInformation("Calculating delivery fee for address: {Address}", deliveryAddress);

            // Geocode the delivery address
            var deliveryLocation = await _googleMapsService.GeocodeAddressAsync(deliveryAddress);

            // Calculate distance from service center
            var distance = await _googleMapsService.CalculateDistanceAsync(
                _serviceCenterLocation.Latitude,
                _serviceCenterLocation.Longitude,
                deliveryLocation.Latitude,
                deliveryLocation.Longitude);

            // Calculate fee: base fee + (distance * fee per km)
            var fee = _baseDeliveryFee + (decimal)distance * _feePerKm;
            
            // Round to nearest 1000 VND
            fee = Math.Round(fee / 1000) * 1000;

            var calculation = $"Base fee: {_baseDeliveryFee:N0} VND + ({distance:F2} km × {_feePerKm:N0} VND/km) = {fee:N0} VND";

            _logger.LogInformation(
                "Delivery fee calculated: {Fee} VND for distance {Distance} km",
                fee, distance);

            return new DeliveryFeeDto
            {
                Fee = fee,
                DistanceKm = distance,
                Calculation = calculation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating delivery fee for address: {Address}", deliveryAddress);
            throw new InvalidOperationException("Failed to calculate delivery fee. Please check the address and try again.", ex);
        }
    }

    /// <summary>
    /// Gets the service center location
    /// </summary>
    public LocationDto GetServiceCenterLocation()
    {
        return new LocationDto
        {
            Latitude = _serviceCenterLocation.Latitude,
            Longitude = _serviceCenterLocation.Longitude,
            Address = _serviceCenterLocation.Address,
            Timestamp = DateTime.UtcNow
        };
    }
}
