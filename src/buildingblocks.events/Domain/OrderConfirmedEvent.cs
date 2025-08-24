namespace BuildingBlocks.Events.Domain
{
    /// <summary>
    /// Represents an individual item within an order that requires inventory processing.
    /// Contains comprehensive product information and quantity details for accurate stock management operations.
    /// </summary>
    /// <remarks>
    /// This class encapsulates all necessary information for the Inventory service to process
    /// stock deductions when an order is confirmed. The frozen unit price ensures financial
    /// consistency even if product prices change after order confirmation.
    /// </remarks>
    public class OrderItemEvent
    {
        /// <summary>
        /// Unique identifier of the product being ordered.
        /// References the Product entity in the Inventory database for stock deduction operations.
        /// </summary>
        /// <value>A GUID that uniquely identifies the product across the system</value>
        public required Guid ProductId { get; set; }

        /// <summary>
        /// Human-readable name of the product for display, logging, and audit purposes.
        /// Provides context for debugging and business operations without requiring product lookups.
        /// </summary>
        /// <value>The product name as it appeared at the time of order confirmation</value>
        public required string ProductName { get; set; }

        /// <summary>
        /// Quantity of the product ordered by the customer.
        /// This exact amount will be debited from the inventory stock levels.
        /// </summary>
        /// <value>A positive integer representing the number of units ordered</value>
        /// <remarks>
        /// This value has already been validated against available stock at order creation time.
        /// However, the Inventory service should still verify availability during processing
        /// to handle edge cases where stock may have changed between order creation and processing.
        /// </remarks>
        public required int Quantity { get; set; }

        /// <summary>
        /// Unit price of the product at the time of order confirmation.
        /// Price is frozen to ensure financial consistency throughout the order lifecycle.
        /// </summary>
        /// <value>The decimal price per unit in the system's base currency</value>
        /// <remarks>
        /// This frozen price protects both customers and the business from price fluctuations
        /// that might occur between order confirmation and fulfillment. It provides a
        /// complete audit trail for financial reconciliation and reporting.
        /// </remarks>
        public required decimal UnitPrice { get; set; }

        /// <summary>
        /// Calculated total price for this line item (Quantity × UnitPrice).
        /// Provides immediate access to the line total for financial calculations and validation.
        /// </summary>
        /// <value>The total cost for this item in the order</value>
        /// <remarks>
        /// This computed property eliminates the need for repeated calculations and reduces
        /// the risk of calculation errors in consuming services. The value is automatically
        /// updated when Quantity or UnitPrice properties change.
        /// </remarks>
        public decimal TotalPrice => Quantity * UnitPrice;
    }

    /// <summary>
    /// Domain event published when an order has been successfully confirmed and requires inventory processing.
    /// This event triggers automatic stock deduction operations in the Inventory service as part of the
    /// event-driven order fulfillment workflow.
    /// </summary>
    /// <remarks>
    /// The OrderConfirmedEvent represents a critical integration point between the Sales and Inventory services.
    /// When published, it indicates that:
    /// 
    /// 1. The order has passed all business validation rules
    /// 2. Payment authorization has been completed (if applicable)
    /// 3. Stock deduction should be performed immediately
    /// 4. The order is committed and cannot be cancelled without compensation
    /// 
    /// Event Processing Guarantees:
    /// - Events are processed exactly once (idempotency)
    /// - Stock deductions are performed within database transactions
    /// - Failed processing triggers automatic retry mechanisms
    /// - All operations are fully auditable with correlation tracking
    /// 
    /// Integration Pattern:
    /// This event follows the Event-Driven Architecture pattern where services communicate
    /// asynchronously through domain events rather than direct API calls. This approach
    /// provides better resilience, scalability, and decoupling between services.
    /// </remarks>
    public class OrderConfirmedEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the confirmed order.
        /// References the Order entity in the Sales database and enables correlation with inventory operations.
        /// </summary>
        /// <value>A GUID that uniquely identifies the order across all services</value>
        /// <remarks>
        /// This identifier is used by the Inventory service to track which orders have been processed
        /// and to maintain audit trails linking stock movements to specific orders.
        /// </remarks>
        public required Guid OrderId { get; set; }

        /// <summary>
        /// Unique identifier of the customer who placed the order.
        /// Enables customer-specific processing and audit trail maintenance.
        /// </summary>
        /// <value>A GUID that uniquely identifies the customer in the system</value>
        /// <remarks>
        /// While not directly used for inventory processing, this information is valuable
        /// for analytics, customer service operations, and potential future business rules
        /// that might depend on customer-specific logic.
        /// </remarks>
        public required Guid CustomerId { get; set; }

        /// <summary>
        /// Total monetary amount of the order for financial tracking and validation.
        /// Provides a quick reference for the order value without calculating from individual items.
        /// </summary>
        /// <value>The total order amount in the system's base currency</value>
        /// <remarks>
        /// This amount should equal the sum of all item totals (Quantity × UnitPrice) and
        /// can be used for validation purposes. It enables financial reconciliation and
        /// reporting without requiring item-level calculations in consuming services.
        /// </remarks>
        public required decimal TotalAmount { get; set; }

        /// <summary>
        /// Collection of items in the order that require inventory processing.
        /// Each item will trigger a corresponding stock deduction operation in the Inventory service.
        /// </summary>
        /// <value>A collection of OrderItemEvent objects representing the order contents</value>
        /// <remarks>
        /// Processing Logic:
        /// - Each item is processed independently for maximum granularity
        /// - Failed item processing doesn't affect other items in the same order
        /// - All item operations are performed within a single database transaction
        /// - Stock validation is performed for each item during processing
        /// 
        /// The collection is guaranteed to contain at least one item as orders cannot
        /// be confirmed without items in the Sales service.
        /// </remarks>
        public required ICollection<OrderItemEvent> Items { get; set; }

        /// <summary>
        /// Current status of the order at the time of event publication.
        /// Provides context about the order state for debugging and audit purposes.
        /// </summary>
        /// <value>A string representing the order status, typically "Confirmed"</value>
        /// <remarks>
        /// Expected Values:
        /// - "Confirmed": Order has been validated and is ready for fulfillment
        /// - Other statuses may be supported in future versions
        /// 
        /// This status helps distinguish between different types of order events
        /// and provides valuable context for monitoring and troubleshooting.
        /// </remarks>
        public required string Status { get; set; }

        /// <summary>
        /// Timestamp when the original order was created by the customer.
        /// Provides temporal context for order processing and business analytics.
        /// </summary>
        /// <value>UTC timestamp of the original order creation</value>
        /// <remarks>
        /// This timestamp differs from the event's OccurredAt property, which represents
        /// when the order was confirmed. The OrderCreatedAt timestamp enables:
        /// 
        /// - Time-to-confirmation analytics
        /// - Order processing SLA monitoring  
        /// - Historical reporting and trend analysis
        /// - Customer behavior pattern recognition
        /// 
        /// All timestamps are stored in UTC for consistency across time zones.
        /// </remarks>
        public required DateTime OrderCreatedAt { get; set; }
    }
}