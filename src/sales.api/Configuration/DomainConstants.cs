namespace SalesApi.Configuration
{
    /// <summary>
    /// Constants for business domain values used throughout the Sales API.
    /// </summary>
    public static class DomainConstants
    {
        public static class OrderStatus
        {
            public const string Pending = "Pending";
            public const string Confirmed = "Confirmed";
            public const string Fulfilled = "Fulfilled";
            public const string Cancelled = "Cancelled";
            public const string Failed = "Failed";
        }

        public static class PaymentStatus
        {
            public const string Pending = "Pending";
            public const string Completed = "Completed";
            public const string Failed = "Failed";
            public const string Refunded = "Refunded";
        }

        public static class EventTypes
        {
            public const string OrderCreated = "OrderCreatedEvent";
            public const string OrderConfirmed = "OrderConfirmedEvent";
            public const string OrderCancelled = "OrderCancelledEvent";
            public const string OrderFulfilled = "OrderFulfilledEvent";
            public const string PaymentProcessed = "PaymentProcessedEvent";
            public const string PaymentFailed = "PaymentFailedEvent";
            public const string StockReserved = "StockReservedEvent";
            public const string StockReleased = "StockReleasedEvent";
        }

        public static class QueueNames
        {
            public const string SalesQueue = "sales.queue";
            public const string InventoryQueue = "inventory.queue";
            public const string NotificationQueue = "notification.queue";
        }

        public static class CorrelationPrefixes
        {
            public const string SalesOutgoing = "sales-out";
            public const string SalesIncoming = "sales-in";
            public const string InventoryOutgoing = "inventory-out";
            public const string InventoryIncoming = "inventory-in";
            public const string GatewayOutgoing = "gateway-out";
        }
    }
}