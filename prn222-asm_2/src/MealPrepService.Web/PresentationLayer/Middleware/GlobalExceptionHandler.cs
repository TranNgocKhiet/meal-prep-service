using System.Net;
using System.Text.Json;
using MealPrepService.BusinessLogicLayer.Exceptions;

namespace MealPrepService.Web.PresentationLayer.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case ValidationException validationEx:
                    response.Message = "Validation failed";
                    response.Details = validationEx.Errors;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case AuthenticationException authEx:
                    response.Message = "Authentication failed";
                    response.Details = authEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;

                case AuthorizationException authzEx:
                    response.Message = "Access denied";
                    response.Details = authzEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    break;

                case NotFoundException notFoundEx:
                    response.Message = "Resource not found";
                    response.Details = notFoundEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case ConstraintViolationException constraintEx:
                    response.Message = "Data constraint violation";
                    response.Details = constraintEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    break;

                case BusinessException businessEx:
                    response.Message = "Business rule violation";
                    response.Details = businessEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case ArgumentException argEx:
                    response.Message = "Invalid argument";
                    response.Details = argEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case InvalidOperationException invalidOpEx:
                    response.Message = "Invalid operation";
                    response.Details = invalidOpEx.Message;
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                default:
                    response.Message = "An internal server error occurred";
                    response.Details = "Please try again later or contact support if the problem persists";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public object? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}