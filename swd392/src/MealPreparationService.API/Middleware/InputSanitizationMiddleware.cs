using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MealPreparationService.API.Middleware;

/// <summary>
/// Middleware to sanitize user inputs to prevent SQL injection and XSS attacks
/// Validates: Requirements 22.4
/// </summary>
public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputSanitizationMiddleware> _logger;

    // SQL injection patterns to detect
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|UNION|DECLARE|CAST|CONVERT)\b)|" +
        @"(--|;|\/\*|\*\/|xp_|sp_|@@|char\(|nchar\(|varchar\(|nvarchar\(|sysobjects|syscolumns)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // XSS patterns to detect
    private static readonly Regex XssPattern = new(
        @"<script|</script|javascript:|onerror=|onload=|onclick=|<iframe|</iframe|eval\(|expression\(|vbscript:",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public InputSanitizationMiddleware(RequestDelegate next, ILogger<InputSanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process POST, PUT, PATCH requests with JSON content
        if ((context.Request.Method == HttpMethods.Post || 
             context.Request.Method == HttpMethods.Put || 
             context.Request.Method == HttpMethods.Patch) &&
            context.Request.ContentType?.Contains("application/json") == true)
        {
            context.Request.EnableBuffering();
            
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                // Check for SQL injection attempts
                if (SqlInjectionPattern.IsMatch(body))
                {
                    _logger.LogWarning(
                        "Potential SQL injection attempt detected from {IpAddress} on {Path}",
                        context.Connection.RemoteIpAddress,
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        success = false,
                        message = "Invalid input detected. Please check your data.",
                        errors = new[] { new { field = "input", message = "Potentially unsafe content detected" } }
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }

                // Check for XSS attempts
                if (XssPattern.IsMatch(body))
                {
                    _logger.LogWarning(
                        "Potential XSS attempt detected from {IpAddress} on {Path}",
                        context.Connection.RemoteIpAddress,
                        context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        success = false,
                        message = "Invalid input detected. Please check your data.",
                        errors = new[] { new { field = "input", message = "Potentially unsafe content detected" } }
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
            }
        }

        // Sanitize query string parameters
        if (context.Request.Query.Any())
        {
            foreach (var param in context.Request.Query)
            {
                var value = param.Value.ToString();
                
                if (SqlInjectionPattern.IsMatch(value) || XssPattern.IsMatch(value))
                {
                    _logger.LogWarning(
                        "Potential attack detected in query parameter '{Parameter}' from {IpAddress}",
                        param.Key,
                        context.Connection.RemoteIpAddress);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    
                    var response = new
                    {
                        success = false,
                        message = "Invalid query parameter detected.",
                        errors = new[] { new { field = param.Key, message = "Potentially unsafe content detected" } }
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// HTML encodes a string to prevent XSS attacks
    /// </summary>
    public static string HtmlEncode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return HtmlEncoder.Default.Encode(input);
    }

    /// <summary>
    /// Sanitizes a string by removing potentially dangerous characters
    /// </summary>
    public static string SanitizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove null bytes first
        input = input.Replace("\0", string.Empty);

        // HTML encode to prevent XSS
        return HtmlEncode(input);
    }
}
