using MealPreparationService.Business.DTOs;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for Google Maps API integration
/// </summary>
public interface IGoogleMapsService
{
    /// <summary>
    /// Calculates distance between two coordinates in kilometers
    /// </summary>
    /// <param name="originLat">Origin latitude</param>
    /// <param name="originLng">Origin longitude</param>
    /// <param name="destLat">Destination latitude</param>
    /// <param name="destLng">Destination longitude</param>
    /// <returns>Distance in kilometers</returns>
    Task<double> CalculateDistanceAsync(double originLat, double originLng, double destLat, double destLng);
    
    /// <summary>
    /// Geocodes an address to coordinates
    /// </summary>
    /// <param name="address">Address to geocode</param>
    /// <returns>Location with latitude and longitude</returns>
    Task<LocationDto> GeocodeAddressAsync(string address);
    
    /// <summary>
    /// Calculates route with estimated time between two locations
    /// </summary>
    /// <param name="start">Start location</param>
    /// <param name="end">End location</param>
    /// <returns>Route information with estimated time</returns>
    Task<RouteDto> CalculateRouteAsync(LocationDto start, LocationDto end);
    
    /// <summary>
    /// Generates map embed URL for displaying location
    /// </summary>
    /// <param name="location">Location to display</param>
    /// <param name="zoom">Zoom level (default 15)</param>
    /// <returns>Google Maps embed URL</returns>
    string GetMapEmbedUrl(LocationDto location, int zoom = 15);
}
