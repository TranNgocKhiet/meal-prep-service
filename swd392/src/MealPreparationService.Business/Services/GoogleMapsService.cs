using MealPreparationService.Business.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service for Google Maps API integration with distance calculation, geocoding, and routing
/// </summary>
public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleMapsService> _logger;
    private readonly string _apiKey;
    private readonly int _timeoutSeconds;
    private const string BaseUrl = "https://maps.googleapis.com/maps/api";

    public GoogleMapsService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleMapsService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("GoogleMaps");
        _logger = logger;
        
        _apiKey = configuration["GoogleMaps:ApiKey"] 
            ?? throw new InvalidOperationException("Google Maps API key is not configured");
        
        _timeoutSeconds = int.TryParse(configuration["GoogleMaps:TimeoutSeconds"], out var timeout) 
            ? timeout : 10;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
        
        _logger.LogInformation("Google Maps service initialized with timeout: {Timeout}s", _timeoutSeconds);
    }

    /// <summary>
    /// Calculates distance between two coordinates in kilometers using Distance Matrix API
    /// Validates: Requirements 21.3
    /// </summary>
    public async Task<double> CalculateDistanceAsync(
        double originLat, double originLng, 
        double destLat, double destLng)
    {
        try
        {
            var requestStartTime = DateTime.UtcNow;
            var origin = $"{originLat},{originLng}";
            var destination = $"{destLat},{destLng}";
            
            var url = $"{BaseUrl}/distancematrix/json" +
                     $"?origins={HttpUtility.UrlEncode(origin)}" +
                     $"&destinations={HttpUtility.UrlEncode(destination)}" +
                     $"&key={_apiKey}";

            _logger.LogInformation(
                "Google Maps API: Distance calculation started | Origin: ({OriginLat},{OriginLng}) | Destination: ({DestLat},{DestLng}) | Timestamp: {Timestamp}", 
                originLat, originLng, destLat, destLng, requestStartTime);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DistanceMatrixResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK")
            {
                _logger.LogError("Google Maps API: Distance Matrix returned error status | Status: {Status}", result?.Status);
                throw new InvalidOperationException($"Google Maps API error: {result?.Status}");
            }

            if (result.Rows == null || result.Rows.Count == 0 || 
                result.Rows[0].Elements == null || result.Rows[0].Elements.Count == 0)
            {
                throw new InvalidOperationException("No distance data returned from Google Maps API");
            }

            var element = result.Rows[0].Elements[0];
            if (element.Status != "OK")
            {
                _logger.LogError("Google Maps API: Distance calculation failed | Status: {Status}", element.Status);
                throw new InvalidOperationException($"Distance calculation failed: {element.Status}");
            }

            // Distance is returned in meters, convert to kilometers
            var distanceKm = element.Distance.Value / 1000.0;
            var requestEndTime = DateTime.UtcNow;
            var duration = (requestEndTime - requestStartTime).TotalMilliseconds;
            
            _logger.LogInformation(
                "Google Maps API: Distance calculated successfully | Distance: {Distance} km | Duration: {Duration}ms | Timestamp: {Timestamp}",
                distanceKm, duration, requestEndTime);
            
            return distanceKm;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Google Maps API: HTTP error calling Distance Matrix API | Error: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("Failed to calculate distance. Please check your internet connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Google Maps API: Request timed out after {Timeout}s", _timeoutSeconds);
            throw new InvalidOperationException($"Distance calculation timed out after {_timeoutSeconds} seconds.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Maps API: Error calculating distance | Error: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("Failed to calculate distance.", ex);
        }
    }

    /// <summary>
    /// Geocodes an address to coordinates using Geocoding API
    /// </summary>
    public async Task<LocationDto> GeocodeAddressAsync(string address)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("Address cannot be empty", nameof(address));
            }

            var url = $"{BaseUrl}/geocode/json" +
                     $"?address={HttpUtility.UrlEncode(address)}" +
                     $"&key={_apiKey}";

            _logger.LogInformation("Geocoding address: {Address}", address);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GeocodeResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK")
            {
                _logger.LogError("Google Maps Geocoding API returned status: {Status}", result?.Status);
                throw new InvalidOperationException($"Geocoding failed: {result?.Status}");
            }

            if (result.Results == null || result.Results.Count == 0)
            {
                throw new InvalidOperationException("No geocoding results found for the address");
            }

            var location = result.Results[0].Geometry.Location;
            var formattedAddress = result.Results[0].FormattedAddress;

            _logger.LogInformation("Address geocoded to: ({Lat},{Lng})", location.Lat, location.Lng);

            return new LocationDto
            {
                Latitude = location.Lat,
                Longitude = location.Lng,
                Address = formattedAddress,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Google Maps Geocoding API");
            throw new InvalidOperationException("Failed to geocode address. Please check your internet connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Google Maps API request timed out after {Timeout}s", _timeoutSeconds);
            throw new InvalidOperationException($"Geocoding timed out after {_timeoutSeconds} seconds.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            throw new InvalidOperationException("Failed to geocode address.", ex);
        }
    }

    /// <summary>
    /// Calculates route with estimated time using Directions API
    /// </summary>
    public async Task<RouteDto> CalculateRouteAsync(LocationDto start, LocationDto end)
    {
        try
        {
            var origin = $"{start.Latitude},{start.Longitude}";
            var destination = $"{end.Latitude},{end.Longitude}";
            
            var url = $"{BaseUrl}/directions/json" +
                     $"?origin={HttpUtility.UrlEncode(origin)}" +
                     $"&destination={HttpUtility.UrlEncode(destination)}" +
                     $"&key={_apiKey}";

            _logger.LogInformation("Calculating route from ({StartLat},{StartLng}) to ({EndLat},{EndLng})", 
                start.Latitude, start.Longitude, end.Latitude, end.Longitude);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DirectionsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "OK")
            {
                _logger.LogError("Google Maps Directions API returned status: {Status}", result?.Status);
                throw new InvalidOperationException($"Route calculation failed: {result?.Status}");
            }

            if (result.Routes == null || result.Routes.Count == 0)
            {
                throw new InvalidOperationException("No routes found");
            }

            var route = result.Routes[0];
            var leg = route.Legs[0];

            // Distance is in meters, convert to kilometers
            var distanceKm = leg.Distance.Value / 1000.0;
            
            // Duration is in seconds, convert to TimeSpan
            var estimatedTime = TimeSpan.FromSeconds(leg.Duration.Value);

            _logger.LogInformation("Route calculated: {Distance} km, {Duration} minutes", 
                distanceKm, estimatedTime.TotalMinutes);

            return new RouteDto
            {
                StartLocation = start,
                EndLocation = end,
                DistanceKm = distanceKm,
                EstimatedTime = estimatedTime,
                RoutePolyline = route.OverviewPolyline?.Points
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Google Maps Directions API");
            throw new InvalidOperationException("Failed to calculate route. Please check your internet connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Google Maps API request timed out after {Timeout}s", _timeoutSeconds);
            throw new InvalidOperationException($"Route calculation timed out after {_timeoutSeconds} seconds.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating route");
            throw new InvalidOperationException("Failed to calculate route.", ex);
        }
    }

    /// <summary>
    /// Generates map embed URL for displaying location
    /// </summary>
    public string GetMapEmbedUrl(LocationDto location, int zoom = 15)
    {
        var url = $"https://www.google.com/maps/embed/v1/view" +
                 $"?key={_apiKey}" +
                 $"&center={location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                 $"&zoom={zoom}";

        _logger.LogDebug("Generated map embed URL for location ({Lat},{Lng})", 
            location.Latitude, location.Longitude);

        return url;
    }

    #region Response Models

    private class DistanceMatrixResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("rows")]
        public List<Row> Rows { get; set; } = new();
    }

    private class Row
    {
        [JsonPropertyName("elements")]
        public List<Element> Elements { get; set; } = new();
    }

    private class Element
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("distance")]
        public Distance Distance { get; set; } = new();
        [JsonPropertyName("duration")]
        public Duration Duration { get; set; } = new();
    }

    private class Distance
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        [JsonPropertyName("value")]
        public int Value { get; set; } // in meters
    }

    private class Duration
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        [JsonPropertyName("value")]
        public int Value { get; set; } // in seconds
    }

    private class GeocodeResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("results")]
        public List<GeocodeResult> Results { get; set; } = new();
    }

    private class GeocodeResult
    {
        [JsonPropertyName("formatted_address")]
        public string FormattedAddress { get; set; } = string.Empty;
        [JsonPropertyName("geometry")]
        public Geometry Geometry { get; set; } = new();
    }

    private class Geometry
    {
        [JsonPropertyName("location")]
        public Location Location { get; set; } = new();
    }

    private class Location
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    private class DirectionsResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("routes")]
        public List<Route> Routes { get; set; } = new();
    }

    private class Route
    {
        [JsonPropertyName("legs")]
        public List<Leg> Legs { get; set; } = new();
        [JsonPropertyName("overview_polyline")]
        public OverviewPolyline? OverviewPolyline { get; set; }
    }

    private class Leg
    {
        [JsonPropertyName("distance")]
        public Distance Distance { get; set; } = new();
        [JsonPropertyName("duration")]
        public Duration Duration { get; set; } = new();
    }

    private class OverviewPolyline
    {
        [JsonPropertyName("points")]
        public string Points { get; set; } = string.Empty;
    }

    #endregion
}
