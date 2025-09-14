using Microsoft.Extensions.Logging;

namespace SalesApi.Extensions
{
    /// <summary>
    /// Extension methods for structured logging throughout the Sales API.
    /// Provides consistent logging patterns for business events.
    /// </summary>
    public static class LoggerExtensions
    {
        // Order-related logging
        public static void LogOrderCreated(this ILogger logger, Guid orderId, Guid customerId)
            => logger.LogInformation("Order {OrderId} created for customer {CustomerId}", orderId, customerId);

        public static void LogOrderConfirmed(this ILogger logger, Guid orderId, decimal totalAmount)
            => logger.LogInformation("Order {OrderId} confirmed with total amount {TotalAmount:C}", orderId, totalAmount);

        public static void LogOrderCancelled(this ILogger logger, Guid orderId, string reason)
            => logger.LogWarning("Order {OrderId} cancelled. Reason: {Reason}", orderId, reason);

        public static void LogOrderFulfilled(this ILogger logger, Guid orderId)
            => logger.LogInformation("Order {OrderId} fulfilled successfully", orderId);

        // Stock-related logging
        public static void LogStockReserved(this ILogger logger, Guid productId, int quantity, Guid orderId)
            => logger.LogInformation("Reserved {Quantity} units of product {ProductId} for order {OrderId}", quantity, productId, orderId);

        public static void LogStockReservationFailed(this ILogger logger, Guid productId, int requestedQuantity, int availableQuantity)
            => logger.LogWarning("Stock reservation failed for product {ProductId}. Requested: {RequestedQuantity}, Available: {AvailableQuantity}", 
                productId, requestedQuantity, availableQuantity);

        public static void LogStockReleased(this ILogger logger, Guid productId, int quantity, Guid orderId)
            => logger.LogInformation("Released {Quantity} units of product {ProductId} from cancelled order {OrderId}", quantity, productId, orderId);

        // Payment-related logging
        public static void LogPaymentProcessed(this ILogger logger, Guid orderId, decimal amount, string paymentMethod)
            => logger.LogInformation("Payment processed for order {OrderId}. Amount: {Amount:C}, Method: {PaymentMethod}", orderId, amount, paymentMethod);

        public static void LogPaymentFailed(this ILogger logger, Guid orderId, decimal amount, string error)
            => logger.LogError("Payment failed for order {OrderId}. Amount: {Amount:C}, Error: {Error}", orderId, amount, error);

        // Event processing logging
        public static void LogEventPublished(this ILogger logger, string eventType, Guid eventId, Guid? relatedEntityId = null)
            => logger.LogInformation("Event {EventType} published with ID {EventId}{RelatedEntity}", 
                eventType, eventId, relatedEntityId.HasValue ? $" for entity {relatedEntityId}" : "");

        public static void LogEventProcessed(this ILogger logger, string eventType, Guid eventId, TimeSpan processingTime)
            => logger.LogInformation("Event {EventType} with ID {EventId} processed in {ProcessingTime}ms", 
                eventType, eventId, processingTime.TotalMilliseconds);

        public static void LogEventProcessingFailed(this ILogger logger, string eventType, Guid eventId, Exception exception)
            => logger.LogError(exception, "Failed to process event {EventType} with ID {EventId}", eventType, eventId);

        // HTTP client logging
        public static void LogExternalApiCall(this ILogger logger, string serviceName, string endpoint, TimeSpan duration)
            => logger.LogInformation("External API call to {ServiceName} endpoint {Endpoint} completed in {Duration}ms", 
                serviceName, endpoint, duration.TotalMilliseconds);

        public static void LogExternalApiError(this ILogger logger, string serviceName, string endpoint, int statusCode, string error)
            => logger.LogError("External API call to {ServiceName} endpoint {Endpoint} failed with status {StatusCode}: {Error}", 
                serviceName, endpoint, statusCode, error);

        // Health check logging
        public static void LogHealthCheckExecuted(this ILogger logger, string healthCheckName, string status, TimeSpan duration)
            => logger.LogInformation("Health check {HealthCheckName} executed with status {Status} in {Duration}ms", 
                healthCheckName, status, duration.TotalMilliseconds);

        // Performance logging
        public static void LogPerformanceMetric(this ILogger logger, string operationName, TimeSpan duration, int itemCount = 0)
        {
            if (itemCount > 0)
                logger.LogInformation("Operation {OperationName} completed in {Duration}ms for {ItemCount} items", 
                    operationName, duration.TotalMilliseconds, itemCount);
            else
                logger.LogInformation("Operation {OperationName} completed in {Duration}ms", 
                    operationName, duration.TotalMilliseconds);
        }

        public static void LogSlowOperation(this ILogger logger, string operationName, TimeSpan duration, TimeSpan threshold)
            => logger.LogWarning("Slow operation detected: {OperationName} took {Duration}ms (threshold: {Threshold}ms)", 
                operationName, duration.TotalMilliseconds, threshold.TotalMilliseconds);
    }
}