using System.Diagnostics;

namespace SalesApi.Middleware
{
    /// <summary>
    /// Middleware for handling correlation IDs in the Sales API.
    /// Ensures request correlation for order processing workflows and stock reservations.
    /// </summary>
    /// <remarks>
    /// This middleware provides observability for critical sales operations:
    /// - Order creation with stock reservation workflows
    /// - Payment processing simulation and validation
    /// - Event publishing for OrderConfirmedEvent and OrderCancelledEvent
    /// - Cross-service communication with Inventory API for reservations
    /// 
    /// The correlation ID enables tracking of:
    /// - Gateway ? Sales API order requests
    /// - Sales API ? Inventory API stock reservation calls
    /// - Event publishing and saga pattern workflows
    /// - End-to-end order processing lifecycle
    /// </remarks>
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationMiddleware> _logger;
        private const string CorrelationIdHeaderName = "X-Correlation-Id";

        public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Processes sales API requests with correlation context.
        /// </summary>
        /// <param name="context">HTTP context for the current request</param>
        /// <returns>Task representing the asynchronous middleware operation</returns>
        /// <remarks>
        /// Sales-Specific Processing:
        /// - Extract correlation ID from Gateway requests
        /// - Set correlation context for order processing workflows
        /// - Log all sales operations with correlation tracking
        /// - Support stock reservation and event publishing workflows
        /// - Track performance of critical business operations
        /// </remarks>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Extract correlation ID (should come from Gateway)
            var correlationId = GetCorrelationId(context.Request);
            
            // Set correlation ID for distributed tracing
            Activity.Current?.SetTag("correlation_id", correlationId);
            Activity.Current?.SetTag("service", "sales");
            
            // Add correlation ID to response headers
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            
            // Set correlation context for logging and operations
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["ServiceName"] = "Sales",
                ["RequestPath"] = context.Request.Path,
                ["RequestMethod"] = context.Request.Method,
                ["UserAgent"] = context.Request.Headers.UserAgent.ToString()
            }))
            {
                _logger.LogInformation(
                    "?? Sales request started: {Method} {Path} | CorrelationId: {CorrelationId}",
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
                    
                    // Log with sales-specific context
                    _logger.LogInformation(
                        "?? Sales request completed: {Method} {Path} | CorrelationId: {CorrelationId} | " +
                        "Duration: {ElapsedMilliseconds}ms | Status: {StatusCode}",
                        context.Request.Method,
                        context.Request.Path,
                        correlationId,
                        stopwatch.ElapsedMilliseconds,
                        context.Response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Extracts correlation ID from request headers with fallback generation.
        /// </summary>
        /// <param name="request">HTTP request to extract correlation ID from</param>
        /// <returns>Valid correlation ID string</returns>
        /// <remarks>
        /// Correlation ID Sources (in priority order):
        /// 1. X-Correlation-Id header from Gateway
        /// 2. Generated correlation ID with 'sales' prefix
        /// 
        /// The correlation ID should typically come from the Gateway
        /// for proper end-to-end request correlation.
        /// </remarks>
        private static string GetCorrelationId(HttpRequest request)
        {
            if (request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId!;
            }

            // Generate new correlation ID for direct requests
            return $"sales-{Guid.NewGuid():N}"; // 'sales' prefix indicates generated by sales
        }
    }

    /// <summary>
    /// Extension methods for registering correlation middleware in Sales API.
    /// </summary>
    public static class CorrelationMiddlewareExtensions
    {
        /// <summary>
        /// Adds correlation middleware to the Sales API pipeline.
        /// </summary>
        /// <param name="builder">Application builder to configure</param>
        /// <returns>Application builder for method chaining</returns>
        public static IApplicationBuilder UseCorrelation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationMiddleware>();
        }
    }
}