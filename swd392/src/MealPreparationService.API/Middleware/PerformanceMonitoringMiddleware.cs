using System.Diagnostics;

namespace MealPreparationService.API.Middleware;

/// <summary>
/// Middleware for monitoring API response times and performance metrics
/// Validates: Requirements 23.1
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private const int SlowRequestThresholdMs = 500; // 500ms threshold per requirement 23.1

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        // Hook into response starting to add header before body is written
        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            context.Response.Headers.Append("X-Response-Time-Ms", elapsed.ToString());
            return Task.CompletedTask;
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            // Log performance metrics
            if (elapsed > SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected | Method: {Method} | Path: {Path} | Status: {StatusCode} | Duration: {Duration}ms | Threshold: {Threshold}ms",
                    requestMethod, requestPath, statusCode, elapsed, SlowRequestThresholdMs);
            }
            else
            {
                _logger.LogDebug(
                    "Request performance | Method: {Method} | Path: {Path} | Status: {StatusCode} | Duration: {Duration}ms",
                    requestMethod, requestPath, statusCode, elapsed);
            }
        }
    }
}
