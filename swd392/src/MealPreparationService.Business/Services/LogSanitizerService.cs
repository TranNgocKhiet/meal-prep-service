using System.Text.Json;
using System.Text.RegularExpressions;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service for sanitizing sensitive data in logs
/// Validates: Requirements 21.7
/// </summary>
public class LogSanitizerService : ILogSanitizerService
{
    // Patterns for sensitive data
    private static readonly Regex PasswordPattern = new(
        @"(password|pwd|passwd)[\s]*[=:]\s*[""']?([^""'\s,}]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ApiKeyPattern = new(
        @"(api[_-]?key|apikey|secret[_-]?key|secretkey|access[_-]?token|accesstoken)[\s]*[=:]\s*[""']?([^""'\s,}]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CreditCardPattern = new(
        @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex EmailPattern = new(
        @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
        RegexOptions.Compiled);

    private static readonly Regex PhonePattern = new(
        @"\b(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly string[] SensitiveFields = new[]
    {
        "password", "passwordhash", "pwd", "passwd",
        "apikey", "api_key", "secretkey", "secret_key",
        "accesstoken", "access_token", "refreshtoken", "refresh_token",
        "creditcard", "credit_card", "cvv", "ssn",
        "authorization", "bearer"
    };

    /// <summary>
    /// Sanitizes sensitive data from a log message
    /// </summary>
    public string SanitizeLogMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Sanitize passwords
        message = PasswordPattern.Replace(message, "$1=***REDACTED***");

        // Sanitize API keys and tokens
        message = ApiKeyPattern.Replace(message, "$1=***REDACTED***");

        // Sanitize credit card numbers
        message = CreditCardPattern.Replace(message, "****-****-****-****");

        // Partially sanitize emails (keep domain)
        message = EmailPattern.Replace(message, m =>
        {
            var email = m.Value;
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                var localPart = email.Substring(0, atIndex);
                var domain = email.Substring(atIndex);
                return $"{localPart.Substring(0, Math.Min(2, localPart.Length))}***{domain}";
            }
            return "***@***";
        });

        // Partially sanitize phone numbers
        message = PhonePattern.Replace(message, m =>
        {
            var phone = m.Value;
            if (phone.Length > 4)
            {
                return "***-***-" + phone.Substring(phone.Length - 4);
            }
            return "***-***-****";
        });

        return message;
    }

    /// <summary>
    /// Sanitizes sensitive data from an object before logging
    /// </summary>
    public object SanitizeObject(object obj)
    {
        if (obj == null)
            return obj;

        try
        {
            // Serialize to JSON
            var json = JsonSerializer.Serialize(obj);
            
            // Parse as JsonDocument
            using var document = JsonDocument.Parse(json);
            var sanitized = SanitizeJsonElement(document.RootElement);
            
            return sanitized;
        }
        catch
        {
            // If serialization fails, return sanitized string representation
            return SanitizeLogMessage(obj.ToString() ?? string.Empty);
        }
    }

    private object SanitizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name;
                    var isSensitive = SensitiveFields.Any(field => 
                        key.Equals(field, StringComparison.OrdinalIgnoreCase));

                    if (isSensitive)
                    {
                        dict[key] = "***REDACTED***";
                    }
                    else
                    {
                        dict[key] = SanitizeJsonElement(property.Value);
                    }
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(SanitizeJsonElement(item));
                }
                return list;

            case JsonValueKind.String:
                return SanitizeLogMessage(element.GetString() ?? string.Empty);

            case JsonValueKind.Number:
                return element.GetDecimal();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null!;

            default:
                return element.ToString() ?? string.Empty;
        }
    }
}
