namespace MealPreparationService.Business.Services;

/// <summary>
/// Service for sanitizing sensitive data in logs
/// Validates: Requirements 21.7
/// </summary>
public interface ILogSanitizerService
{
    /// <summary>
    /// Sanitizes sensitive data from a log message
    /// </summary>
    string SanitizeLogMessage(string message);
    
    /// <summary>
    /// Sanitizes sensitive data from an object before logging
    /// </summary>
    object SanitizeObject(object obj);
}
