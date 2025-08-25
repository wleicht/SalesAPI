using System.Diagnostics;

namespace InventoryApi.Middleware
{
    /// <summary>
    /// Middleware for handling correlation IDs in the Inventory API.
    /// Ensures request correlation for stock reservation workflows and event processing.
    /// </summary>
    /// <remarks>
    /// This middleware provides observability for critical inventory operations:
    /// - Stock reservation operations with end-to-end tracing
    /// - Product management operations with user correlation
    /// - Event-driven stock deduction workflows
    /// - Cross-service communication from Sales API and Gateway
    /// 
    /// The correlation ID enables tracking of:
    /// - Gateway ? Inventory API requests
    /// - Sales API ? Inventory API stock reservation calls
    /// - Event processing for OrderConfirmedEvent and OrderCancelledEvent
    /// - Stock reservation lifecycle (Reserved ? Debited/Released)
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
        /// Processes inventory API requests with correlation context.
        /// </summary>
        /// <param name="context">HTTP context for the current request</param>
        /// <returns>Task representing the asynchronous middleware operation</returns>
        /// <remarks>
        /// Inventory-Specific Processing:
        /// - Extract correlation ID from Gateway or Sales API
        /// - Set correlation context for stock reservation operations
        /// - Log all inventory operations with correlation
        /// - Support event-driven workflows with correlation propagation
        /// - Track performance of critical inventory operations
        /// </remarks>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Extract correlation ID (should come from Gateway or Sales API)
            var correlationId = GetCorrelationId(context.Request);
            
            // Set correlation ID for distributed tracing
            Activity.Current?.SetTag("correlation_id", correlationId);
            Activity.Current?.SetTag("service", "inventory");
            
            // Add correlation ID to response headers
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            
            // Set correlation context for logging and operations
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["ServiceName"] = "Inventory",
                ["RequestPath"] = context.Request.Path,
                ["RequestMethod"] = context.Request.Method,
                ["UserAgent"] = context.Request.Headers.UserAgent.ToString()
            }))
            {
                _logger.LogInformation(
                    "?? Inventory request started: {Method} {Path} | CorrelationId: {CorrelationId}",
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
                    
                    // Log with inventory-specific context
                    _logger.LogInformation(
                        "?? Inventory request completed: {Method} {Path} | CorrelationId: {CorrelationId} | " +
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
        /// 1. X-Correlation-Id header from Gateway/Sales API
        /// 2. Generated correlation ID with 'inv' prefix
        /// 
        /// The correlation ID should typically come from upstream services
        /// (Gateway or Sales API) for proper request correlation.
        /// </remarks>
        private static string GetCorrelationId(HttpRequest request)
        {
            if (request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId!;
            }

            // Generate new correlation ID for direct requests
            return $"inv-{Guid.NewGuid():N}"; // 'inv' prefix indicates generated by inventory
        }
    }

    /// <summary>
    /// Extension methods for registering correlation middleware in Inventory API.
    /// </summary>
    public static class CorrelationMiddlewareExtensions
    {
        /// <summary>
        /// Adds correlation middleware to the Inventory API pipeline.
        /// </summary>
        /// <param name="builder">Application builder to configure</param>
        /// <returns>Application builder for method chaining</returns>
        public static IApplicationBuilder UseCorrelation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationMiddleware>();
        }
    }
}