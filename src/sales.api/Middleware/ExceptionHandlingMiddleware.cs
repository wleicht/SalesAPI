using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SalesApi.Middleware
{
    /// <summary>
    /// Global exception handling middleware that catches unhandled exceptions
    /// and converts them to appropriate HTTP responses with structured error information.
    /// </summary>
    /// <remarks>
    /// This middleware serves as the last line of defense against unhandled exceptions,
    /// ensuring that:
    /// - No sensitive error details are leaked to clients
    /// - All errors are properly logged with correlation IDs
    /// - Consistent error response format is maintained
    /// - Different exception types are handled appropriately
    /// </remarks>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                var correlationId = GetCorrelationId(context);
                
                _logger.LogError(ex, 
                    "?? Unhandled exception occurred | Path: {Path} | Method: {Method} | CorrelationId: {CorrelationId}",
                    context.Request.Path,
                    context.Request.Method,
                    correlationId);

                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, title, detail) = GetErrorResponse(exception);
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = statusCode,
                Instance = context.Request.Path
            };

            // Add correlation ID for tracing
            problemDetails.Extensions["correlationId"] = correlationId;
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var jsonResponse = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static (int statusCode, string title, string detail) GetErrorResponse(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException ex => (
                    (int)HttpStatusCode.BadRequest,
                    "Bad Request",
                    $"Required parameter '{ex.ParamName}' is missing"
                ),
                
                ArgumentException => (
                    (int)HttpStatusCode.BadRequest,
                    "Bad Request",
                    "Invalid argument provided"
                ),
                
                InvalidOperationException => (
                    (int)HttpStatusCode.UnprocessableEntity,
                    "Business Rule Violation",
                    "The requested operation cannot be performed"
                ),
                
                TaskCanceledException when exception.InnerException is TimeoutException => (
                    (int)HttpStatusCode.GatewayTimeout,
                    "Request Timeout",
                    "The request timed out"
                ),
                
                HttpRequestException => (
                    (int)HttpStatusCode.BadGateway,
                    "External Service Error",
                    "An external service is unavailable"
                ),
                
                UnauthorizedAccessException => (
                    (int)HttpStatusCode.Forbidden,
                    "Access Denied",
                    "You do not have permission to access this resource"
                ),
                
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred while processing your request"
                )
            };
        }

        private static string GetCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) 
                && !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId!;
            }

            return $"error-{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// Extension method for registering the exception handling middleware.
    /// </summary>
    public static class ExceptionHandlingMiddlewareExtensions
    {
        /// <summary>
        /// Registers the global exception handling middleware.
        /// This should be one of the first middleware registered in the pipeline.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <returns>The application builder for method chaining</returns>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}