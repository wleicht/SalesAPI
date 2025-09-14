using Microsoft.Extensions.Logging;

namespace Gateway.Extensions
{
    /// <summary>
    /// Extension methods for structured logging throughout the Gateway API.
    /// Provides consistent logging patterns for gateway operations.
    /// </summary>
    public static class LoggerExtensions
    {
        // Authentication logging
        public static void LogTokenGenerated(this ILogger logger, string username, TimeSpan expiresIn)
            => logger.LogInformation("JWT token generated for user {Username}, expires in {ExpiresIn}", 
                username, expiresIn);

        public static void LogAuthenticationFailed(this ILogger logger, string username, string reason)
            => logger.LogWarning("Authentication failed for user {Username}: {Reason}", username, reason);

        public static void LogTokenValidated(this ILogger logger, string username, string tokenId)
            => logger.LogInformation("JWT token validated for user {Username}, token ID: {TokenId}", username, tokenId);

        public static void LogTokenExpired(this ILogger logger, string username, DateTime expiredAt)
            => logger.LogWarning("JWT token expired for user {Username} at {ExpiredAt}", username, expiredAt);

        // Reverse proxy logging
        public static void LogProxyRequest(this ILogger logger, string method, string path, string destination)
            => logger.LogInformation("Proxying {Method} {Path} to {Destination}", method, path, destination);

        public static void LogProxyResponse(this ILogger logger, string method, string path, int statusCode, TimeSpan duration)
            => logger.LogInformation("Proxy response {Method} {Path} returned {StatusCode} in {Duration}ms", 
                method, path, statusCode, duration.TotalMilliseconds);

        public static void LogProxyError(this ILogger logger, string method, string path, string destination, Exception exception)
            => logger.LogError(exception, "Proxy error {Method} {Path} to {Destination}", method, path, destination);

        public static void LogUpstreamServiceUnavailable(this ILogger logger, string serviceName, string endpoint)
            => logger.LogError("Upstream service {ServiceName} unavailable at {Endpoint}", serviceName, endpoint);

        // Rate limiting logging
        public static void LogRateLimitExceeded(this ILogger logger, string clientId, string endpoint, int requestCount, int limit)
            => logger.LogWarning("Rate limit exceeded for client {ClientId} on {Endpoint}: {RequestCount}/{Limit} requests", 
                clientId, endpoint, requestCount, limit);

        public static void LogRateLimitReset(this ILogger logger, string clientId, string endpoint)
            => logger.LogInformation("Rate limit reset for client {ClientId} on {Endpoint}", clientId, endpoint);

        // Health check logging
        public static void LogHealthCheckExecuted(this ILogger logger, string healthCheckName, string status, TimeSpan duration)
            => logger.LogInformation("Health check {HealthCheckName} executed with status {Status} in {Duration}ms", 
                healthCheckName, status, duration.TotalMilliseconds);

        public static void LogUpstreamHealthCheck(this ILogger logger, string serviceName, string status, TimeSpan responseTime)
            => logger.LogInformation("Upstream service {ServiceName} health check: {Status} (response time: {ResponseTime}ms)", 
                serviceName, status, responseTime.TotalMilliseconds);

        // CORS logging
        public static void LogCorsRequest(this ILogger logger, string origin, string method, bool isPreflightRequest)
        {
            if (isPreflightRequest)
                logger.LogInformation("CORS preflight request from {Origin} for {Method}", origin, method);
            else
                logger.LogInformation("CORS request from {Origin} using {Method}", origin, method);
        }

        public static void LogCorsBlocked(this ILogger logger, string origin, string reason)
            => logger.LogWarning("CORS request blocked from {Origin}: {Reason}", origin, reason);

        // Load balancing logging
        public static void LogLoadBalancerDestination(this ILogger logger, string clusterId, string destinationId, string address)
            => logger.LogInformation("Load balancer selected destination {DestinationId} ({Address}) for cluster {ClusterId}", 
                destinationId, address, clusterId);

        public static void LogLoadBalancerHealthy(this ILogger logger, string destinationId, int healthyCount, int totalCount)
            => logger.LogInformation("Destination {DestinationId} healthy ({HealthyCount}/{TotalCount} destinations available)", 
                destinationId, healthyCount, totalCount);

        public static void LogLoadBalancerUnhealthy(this ILogger logger, string destinationId, string reason)
            => logger.LogWarning("Destination {DestinationId} marked unhealthy: {Reason}", destinationId, reason);

        // Request correlation logging
        public static void LogRequestCorrelation(this ILogger logger, string correlationId, string method, string path)
            => logger.LogInformation("Request {Method} {Path} assigned correlation ID {CorrelationId}", 
                method, path, correlationId);

        public static void LogCorrelationPropagated(this ILogger logger, string correlationId, string upstreamService)
            => logger.LogInformation("Correlation ID {CorrelationId} propagated to upstream service {UpstreamService}", 
                correlationId, upstreamService);

        // Performance logging
        public static void LogPerformanceMetric(this ILogger logger, string operationName, TimeSpan duration, int? itemCount = null)
        {
            if (itemCount.HasValue)
                logger.LogInformation("Operation {OperationName} completed in {Duration}ms for {ItemCount} items", 
                    operationName, duration.TotalMilliseconds, itemCount.Value);
            else
                logger.LogInformation("Operation {OperationName} completed in {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
        }

        public static void LogSlowRequest(this ILogger logger, string method, string path, TimeSpan duration, TimeSpan threshold)
            => logger.LogWarning("Slow request detected: {Method} {Path} took {Duration}ms (threshold: {Threshold}ms)", 
                method, path, duration.TotalMilliseconds, threshold.TotalMilliseconds);

        // Circuit breaker logging
        public static void LogCircuitBreakerOpened(this ILogger logger, string serviceName, string reason)
            => logger.LogWarning("Circuit breaker opened for service {ServiceName}: {Reason}", serviceName, reason);

        public static void LogCircuitBreakerClosed(this ILogger logger, string serviceName)
            => logger.LogInformation("Circuit breaker closed for service {ServiceName}", serviceName);

        public static void LogCircuitBreakerHalfOpen(this ILogger logger, string serviceName)
            => logger.LogInformation("Circuit breaker half-open for service {ServiceName}", serviceName);
    }
}