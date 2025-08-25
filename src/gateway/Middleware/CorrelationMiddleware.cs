using System.Diagnostics;

namespace Gateway.Middleware
{
    /// <summary>
    /// Middleware responsible for handling correlation IDs across the request pipeline.
    /// Ensures that all requests have a unique correlation ID for distributed tracing
    /// and cross-service request tracking.
    /// </summary>
    /// <remarks>
    /// This middleware provides the foundation for observability by:
    /// - Generating unique correlation IDs for incoming requests
    /// - Accepting existing correlation IDs from clients
    /// - Propagating correlation IDs to downstream services
    /// - Adding correlation IDs to response headers for debugging
    /// - Setting correlation context for logging throughout the request
    /// 
    /// The correlation ID enables end-to-end request tracing across:
    /// - Gateway ? Inventory API
    /// - Gateway ? Sales API
    /// - Sales API ? Inventory API (via stock reservations)
    /// - Event publishing and consumption workflows
    /// </remarks>
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationMiddleware> _logger;
        private const string CorrelationIdHeaderName = "X-Correlation-Id";

        /// <summary>
        /// Initializes the correlation middleware with required dependencies.
        /// </summary>
        /// <param name="next">Next middleware in the pipeline</param>
        /// <param name="logger">Logger for middleware operations</param>
        public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Processes the incoming request to establish correlation context.
        /// </summary>
        /// <param name="context">HTTP context for the current request</param>
        /// <returns>Task representing the asynchronous middleware operation</returns>
        /// <remarks>
        /// Processing Flow:
        /// 1. Extract or generate correlation ID from request headers
        /// 2. Set correlation ID in current activity for distributed tracing
        /// 3. Add correlation ID to response headers for client debugging
        /// 4. Set correlation context for downstream logging
        /// 5. Continue request pipeline with correlation context
        /// 6. Log request completion with correlation and timing information
        /// 
        /// Header Behavior:
        /// - If X-Correlation-Id header exists: Use provided value
        /// - If no header present: Generate new GUID-based correlation ID
        /// - Always add correlation ID to response headers
        /// - Validate correlation ID format for security
        /// </remarks>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Extract or generate correlation ID
            var correlationId = GetOrCreateCorrelationId(context.Request);
            
            // Set correlation ID for distributed tracing
            Activity.Current?.SetTag("correlation_id", correlationId);
            
            // Add correlation ID to response headers for debugging
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            
            // Set correlation context for logging (Serilog will pick this up)
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["RequestPath"] = context.Request.Path,
                ["RequestMethod"] = context.Request.Method,
                ["UserAgent"] = context.Request.Headers.UserAgent.ToString()
            }))
            {
                _logger.LogInformation(
                    "Request started: {Method} {Path} with CorrelationId {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    correlationId);

                try
                {
                    // Continue with the request pipeline
                    await _next(context);
                }
                finally
                {
                    stopwatch.Stop();
                    
                    _logger.LogInformation(
                        "Request completed: {Method} {Path} with CorrelationId {CorrelationId} " +
                        "in {ElapsedMilliseconds}ms with status {StatusCode}",
                        context.Request.Method,
                        context.Request.Path,
                        correlationId,
                        stopwatch.ElapsedMilliseconds,
                        context.Response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Extracts correlation ID from request headers or generates a new one.
        /// </summary>
        /// <param name="request">HTTP request to extract correlation ID from</param>
        /// <returns>Valid correlation ID string</returns>
        /// <remarks>
        /// Correlation ID Rules:
        /// - Accept existing X-Correlation-Id header if present and valid
        /// - Generate new GUID-based ID if no header or invalid format
        /// - Ensure correlation ID is always present and valid
        /// - Support both GUID and string formats for flexibility
        /// - Validate length and format for security
        /// </remarks>
        private static string GetOrCreateCorrelationId(HttpRequest request)
        {
            if (request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
                !string.IsNullOrWhiteSpace(correlationId) &&
                IsValidCorrelationId(correlationId!))
            {
                return correlationId!;
            }

            // Generate new correlation ID
            return $"gtw-{Guid.NewGuid():N}"; // 'gtw' prefix indicates generated by gateway
        }

        /// <summary>
        /// Validates correlation ID format for security and consistency.
        /// </summary>
        /// <param name="correlationId">Correlation ID to validate</param>
        /// <returns>True if correlation ID is valid, false otherwise</returns>
        /// <remarks>
        /// Validation Rules:
        /// - Length between 8 and 128 characters
        /// - Contains only alphanumeric characters, hyphens, and underscores
        /// - Not null or whitespace
        /// - Prevents injection attacks via correlation header
        /// </remarks>
        private static bool IsValidCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                return false;
                
            if (correlationId.Length < 8 || correlationId.Length > 128)
                return false;
                
            // Allow alphanumeric, hyphens, and underscores only
            return correlationId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }
    }

    /// <summary>
    /// Extension methods for registering correlation middleware.
    /// </summary>
    public static class CorrelationMiddlewareExtensions
    {
        /// <summary>
        /// Adds correlation middleware to the application pipeline.
        /// Should be added early in the pipeline to ensure all requests have correlation context.
        /// </summary>
        /// <param name="builder">Application builder to configure</param>
        /// <returns>Application builder for method chaining</returns>
        public static IApplicationBuilder UseCorrelation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationMiddleware>();
        }
    }
}