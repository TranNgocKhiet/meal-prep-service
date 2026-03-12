using System.Net;
using System.Text.Json;
using MealPreparationService.Business.Services;

namespace MealPreparationService.API.Middleware;

/// <summary>
/// Middleware for handling errors with sensitive data sanitization
/// Validates: Requirements 21.1, 21.7
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ILogSanitizerService logSanitizer)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Sanitize exception message and stack trace before logging
            var sanitizedMessage = logSanitizer.SanitizeLogMessage(ex.Message);
            var sanitizedStackTrace = logSanitizer.SanitizeLogMessage(ex.StackTrace ?? string.Empty);
            
            // Log with full stack trace
            _logger.LogError(ex, 
                "Unhandled exception occurred: {Message} | StackTrace: {StackTrace}", 
                sanitizedMessage, sanitizedStackTrace);
            
            await HandleExceptionAsync(context, ex, logSanitizer);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, ILogSanitizerService logSanitizer)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Sanitize error message before sending to client
        var sanitizedMessage = logSanitizer.SanitizeLogMessage(exception.Message);

        var response = new
        {
            success = false,
            message = "An error occurred while processing your request.",
            errors = new[] { new { field = "general", message = "An internal error occurred. Please try again later." } }
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}
