namespace InventoryApi.Configuration
{
    /// <summary>
    /// Constants for business domain values used throughout the Inventory API.
    /// </summary>
    public static class DomainConstants
    {
        public static class ProductStatus
        {
            public const string Active = "Active";
            public const string Inactive = "Inactive";
            public const string Discontinued = "Discontinued";
            public const string OutOfStock = "OutOfStock";
        }

        public static class ReservationStatus
        {
            public const string Active = "Active";
            public const string Expired = "Expired";
            public const string Completed = "Completed";
            public const string Cancelled = "Cancelled";
        }

        public static class StockMovementTypes
        {
            public const string Purchase = "Purchase";
            public const string Sale = "Sale";
            public const string Adjustment = "Adjustment";
            public const string Return = "Return";
            public const string Reservation = "Reservation";
            public const string Release = "Release";
        }

        public static class EventTypes
        {
            public const string OrderConfirmed = "OrderConfirmedEvent";
            public const string OrderCancelled = "OrderCancelledEvent";
            public const string StockDebited = "StockDebitedEvent";
            public const string StockReserved = "StockReservedEvent";
            public const string StockReleased = "StockReleasedEvent";
            public const string ProductCreated = "ProductCreatedEvent";
            public const string ProductUpdated = "ProductUpdatedEvent";
        }

        public static class QueueNames
        {
            public const string InventoryQueue = "inventory.queue";
            public const string SalesQueue = "sales.queue";
            public const string NotificationQueue = "notification.queue";
        }

        public static class CorrelationPrefixes
        {
            public const string InventoryOutgoing = "inventory-out";
            public const string InventoryIncoming = "inventory-in";
        }
    }
}