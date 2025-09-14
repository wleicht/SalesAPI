using Microsoft.Extensions.Logging;

namespace InventoryApi.Extensions
{
    /// <summary>
    /// Extension methods for structured logging throughout the Inventory API.
    /// Provides consistent logging patterns for business events.
    /// </summary>
    public static class LoggerExtensions
    {
        // Stock-related logging
        public static void LogStockUpdated(this ILogger logger, Guid productId, int oldQuantity, int newQuantity)
            => logger.LogInformation("Stock updated for product {ProductId}: {OldQuantity} ? {NewQuantity}", 
                productId, oldQuantity, newQuantity);

        public static void LogStockReserved(this ILogger logger, Guid productId, int quantity, Guid reservationId)
            => logger.LogInformation("Reserved {Quantity} units of product {ProductId} with reservation {ReservationId}", 
                quantity, productId, reservationId);

        public static void LogStockReservationFailed(this ILogger logger, Guid productId, int requestedQuantity, int availableQuantity)
            => logger.LogWarning("Stock reservation failed for product {ProductId}. Requested: {RequestedQuantity}, Available: {AvailableQuantity}", 
                productId, requestedQuantity, availableQuantity);

        public static void LogStockReleased(this ILogger logger, Guid productId, int quantity, Guid reservationId)
            => logger.LogInformation("Released {Quantity} units of product {ProductId} from reservation {ReservationId}", 
                quantity, productId, reservationId);

        public static void LogStockDebited(this ILogger logger, Guid productId, int quantity, Guid orderId)
            => logger.LogInformation("Debited {Quantity} units from product {ProductId} for order {OrderId}", 
                quantity, productId, orderId);

        // Product management logging
        public static void LogProductCreated(this ILogger logger, Guid productId, string productName, decimal price)
            => logger.LogInformation("Product {ProductName} created with ID {ProductId} and price {Price:C}", 
                productName, productId, price);

        public static void LogProductUpdated(this ILogger logger, Guid productId, string productName)
            => logger.LogInformation("Product {ProductName} with ID {ProductId} updated", productName, productId);

        public static void LogProductDeleted(this ILogger logger, Guid productId, string productName)
            => logger.LogWarning("Product {ProductName} with ID {ProductId} deleted", productName, productId);

        public static void LogProductNotFound(this ILogger logger, Guid productId)
            => logger.LogWarning("Product with ID {ProductId} not found", productId);

        // Event processing logging
        public static void LogEventReceived(this ILogger logger, string eventType, Guid eventId, Guid? relatedEntityId = null)
            => logger.LogInformation("Event {EventType} with ID {EventId} received{RelatedEntity}", 
                eventType, eventId, relatedEntityId.HasValue ? $" for entity {relatedEntityId}" : "");

        public static void LogEventProcessed(this ILogger logger, string eventType, Guid eventId, TimeSpan processingTime)
            => logger.LogInformation("Event {EventType} with ID {EventId} processed in {ProcessingTime}ms", 
                eventType, eventId, processingTime.TotalMilliseconds);

        public static void LogEventProcessingFailed(this ILogger logger, string eventType, Guid eventId, Exception exception)
            => logger.LogError(exception, "Failed to process event {EventType} with ID {EventId}", eventType, eventId);

        public static void LogEventAlreadyProcessed(this ILogger logger, string eventType, Guid eventId)
            => logger.LogInformation("Event {EventType} with ID {EventId} already processed (idempotency)", eventType, eventId);

        // Order event specific logging
        public static void LogOrderConfirmedEventProcessing(this ILogger logger, Guid orderId, int itemCount)
            => logger.LogInformation("Processing OrderConfirmedEvent for order {OrderId} with {ItemCount} items", 
                orderId, itemCount);

        public static void LogOrderStockDeduction(this ILogger logger, Guid orderId, Guid productId, int quantity, int remainingStock)
            => logger.LogInformation("Order {OrderId}: Debited {Quantity} units from product {ProductId}, remaining stock: {RemainingStock}", 
                orderId, quantity, productId, remainingStock);

        public static void LogOrderStockDeductionFailed(this ILogger logger, Guid orderId, Guid productId, int requestedQuantity, int availableStock)
            => logger.LogError("Order {OrderId}: Failed to debit {RequestedQuantity} units from product {ProductId}, available stock: {AvailableStock}", 
                orderId, requestedQuantity, productId, availableStock);

        // Performance and monitoring logging
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

        // Health check logging
        public static void LogHealthCheckExecuted(this ILogger logger, string healthCheckName, string status, TimeSpan duration)
            => logger.LogInformation("Health check {HealthCheckName} executed with status {Status} in {Duration}ms", 
                healthCheckName, status, duration.TotalMilliseconds);

        // Database operation logging
        public static void LogDatabaseOperation(this ILogger logger, string operation, int affectedRows, TimeSpan duration)
            => logger.LogInformation("Database operation {Operation} affected {AffectedRows} rows in {Duration}ms", 
                operation, affectedRows, duration.TotalMilliseconds);

        public static void LogDatabaseConnectionIssue(this ILogger logger, string operation, Exception exception)
            => logger.LogError(exception, "Database connection issue during {Operation}", operation);
    }
}