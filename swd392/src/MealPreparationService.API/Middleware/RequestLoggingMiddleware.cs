using System.Diagnostics;
using System.Security.Claims;
using MealPreparationService.Business.Services;

namespace MealPreparationService.API.Middleware;

/// <summary>
/// Middleware for logging requests with sensitive data sanitization
/// Validates: Requirements 21.1, 21.2, 21.3, 21.4, 21.5, 21.6, 21.7
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILogSanitizerService logSanitizer)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var timestamp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

        // Sanitize path before logging (in case it contains sensitive data)
        var sanitizedPath = logSanitizer.SanitizeLogMessage(requestPath.ToString());

        _logger.LogInformation(
            "Incoming request: {Method} {Path} | User: {UserId} | Timestamp: {Timestamp}",
            requestMethod, sanitizedPath, userId, timestamp);

        // Sanitize query string if present
        if (context.Request.QueryString.HasValue)
        {
            var sanitizedQuery = logSanitizer.SanitizeLogMessage(context.Request.QueryString.Value ?? string.Empty);
            _logger.LogDebug("Query string: {QueryString}", sanitizedQuery);
        }

        // Sanitize headers (exclude sensitive ones)
        var sanitizedHeaders = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            if (!IsSensitiveHeader(header.Key))
            {
                sanitizedHeaders[header.Key] = logSanitizer.SanitizeLogMessage(header.Value.ToString());
            }
        }
        _logger.LogDebug("Request headers: {Headers}", sanitizedHeaders);

        await _next(context);

        stopwatch.Stop();
        var statusCode = context.Response.StatusCode;
        var elapsed = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Request completed: {Method} {Path} | User: {UserId} | Status: {StatusCode} | Duration: {Duration}ms | Timestamp: {Timestamp}",
            requestMethod, sanitizedPath, userId, statusCode, elapsed, timestamp);
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key",
            "X-Auth-Token"
        };

        return sensitiveHeaders.Any(h => h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
    }
}

