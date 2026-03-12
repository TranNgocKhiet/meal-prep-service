namespace MealPreparationService.Business.DTOs;

/// <summary>
/// Represents a geographic location with coordinates
/// </summary>
public class LocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Address { get; set; }
}

/// <summary>
/// Represents a route between two locations
/// </summary>
public class RouteDto
{
    public LocationDto StartLocation { get; set; } = null!;
    public LocationDto EndLocation { get; set; } = null!;
    public double DistanceKm { get; set; }
    public TimeSpan EstimatedTime { get; set; }
    public string? RoutePolyline { get; set; }
}

/// <summary>
/// Represents distance validation result
/// </summary>
public class DistanceValidationDto
{
    public bool IsValid { get; set; }
    public double DistanceKm { get; set; }
    public double MaxDistanceKm { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents delivery fee calculation result
/// </summary>
public class DeliveryFeeDto
{
    public decimal Fee { get; set; }
    public double DistanceKm { get; set; }
    public string Calculation { get; set; } = string.Empty;
}
