using BuildingBlocks.Events.Domain;
using SalesApi.Domain.ValueObjects;

namespace SalesApi.Domain.DomainEvents
{
    /// <summary>
    /// Domain event published when a new order is created in the sales system.
    /// Notifies other bounded contexts about order creation for integration workflows,
    /// inventory planning, customer communication, and analytics processing.
    /// </summary>
    /// <remarks>
    /// This event serves as the initial notification in the order lifecycle and enables:
    /// 
    /// Integration Scenarios:
    /// - Customer notification services for order confirmation
    /// - Inventory planning and demand forecasting systems
    /// - Analytics and reporting systems for business intelligence
    /// - Audit logging systems for compliance and troubleshooting
    /// 
    /// Business Process Triggers:
    /// - Customer communication workflows (confirmation emails)
    /// - Inventory monitoring and replenishment alerts
    /// - Fraud detection and security validation processes
    /// - Marketing analytics and customer behavior tracking
    /// 
    /// Event Design Principles:
    /// - Contains complete order information for autonomous processing
    /// - Immutable event data for audit trail consistency
    /// - Rich context information for downstream decision making
    /// - Self-contained data to minimize external dependencies
    /// 
    /// The event follows Domain-Driven Design principles by capturing the business
    /// significance of order creation and enabling loose coupling between domains.
    /// </remarks>
    public class OrderCreatedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the created order.
        /// Primary key for order tracking and cross-system integration.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the customer who placed the order.
        /// Enables customer-specific processing and relationship management.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Total monetary amount of the order including all items.
        /// Provides immediate financial context for downstream processing.
        /// </summary>
        public Money TotalAmount { get; set; }

        /// <summary>
        /// Current status of the order at the time of creation.
        /// Typically "Pending" for newly created orders requiring further processing.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Collection of items included in the order.
        /// Provides complete order composition for inventory and fulfillment processing.
        /// </summary>
        public List<OrderItemData> Items { get; set; } = new();

        /// <summary>
        /// UTC timestamp when the order was created in the system.
        /// Provides temporal context for analytics and business process coordination.
        /// </summary>
        public DateTime OrderCreatedAt { get; set; }

        /// <summary>
        /// Default constructor for serialization frameworks.
        /// Initializes event with current timestamp and new event ID.
        /// </summary>
        public OrderCreatedDomainEvent()
        {
            OrderCreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a domain event from an Order entity with complete order information.
        /// Extracts relevant data while maintaining event immutability and self-containment.
        /// </summary>
        /// <param name="order">Order entity containing the business data</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <remarks>
        /// Data Extraction Strategy:
        /// - Captures order state at event creation time
        /// - Converts order items to event-specific data structures
        /// - Preserves monetary information with currency details
        /// - Includes correlation context for distributed tracing
        /// 
        /// Event Consistency:
        /// - Event data represents a consistent snapshot of the order
        /// - Immutable after creation to ensure audit trail integrity
        /// - Self-contained to minimize dependencies in event consumers
        /// - Rich enough for autonomous processing in downstream systems
        /// </remarks>
        public OrderCreatedDomainEvent(Entities.Order order, string? correlationId = null)
        {
            OrderId = order.Id;
            CustomerId = order.CustomerId;
            TotalAmount = Money.FromAmount(order.TotalAmount);
            Status = order.Status;
            OrderCreatedAt = order.CreatedAt;
            CorrelationId = correlationId;

            Items = order.Items.Select(item => new OrderItemData
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = Money.FromAmount(item.UnitPrice),
                TotalPrice = Money.FromAmount(item.TotalPrice)
            }).ToList();
        }
    }

    /// <summary>
    /// Domain event published when an order is confirmed and ready for fulfillment.
    /// Triggers critical business processes including inventory allocation, payment processing,
    /// and fulfillment pipeline activation across multiple bounded contexts.
    /// </summary>
    /// <remarks>
    /// Order confirmation represents a critical milestone in the order lifecycle:
    /// 
    /// Business Process Implications:
    /// - Inventory reservations should be converted to firm allocations
    /// - Payment authorization should be captured or processed
    /// - Fulfillment and shipping processes should be initiated
    /// - Customer communications should confirm order acceptance
    /// 
    /// Cross-Domain Integration:
    /// - Inventory domain: Convert reservations to allocations
    /// - Payment domain: Process final payment capture
    /// - Fulfillment domain: Begin order preparation and shipping
    /// - Customer service: Update order status and notifications
    /// 
    /// Event Timing:
    /// - Published after all pre-fulfillment validations complete
    /// - Indicates commitment to fulfill the order as specified
    /// - Triggers time-sensitive processes requiring prompt action
    /// - Marks transition from order assembly to order execution
    /// 
    /// The event design ensures that downstream systems receive authoritative
    /// notification when orders are ready for execution and fulfillment.
    /// </remarks>
    public class OrderConfirmedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the confirmed order.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the customer who placed the confirmed order.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Total monetary amount of the confirmed order.
        /// </summary>
        public Money TotalAmount { get; set; }

        /// <summary>
        /// Collection of items in the confirmed order.
        /// Critical for inventory allocation and fulfillment processing.
        /// </summary>
        public List<OrderItemData> Items { get; set; } = new();

        /// <summary>
        /// UTC timestamp when the order was confirmed.
        /// </summary>
        public DateTime ConfirmedAt { get; set; }

        /// <summary>
        /// Identifier of the user or system that confirmed the order.
        /// </summary>
        public string ConfirmedBy { get; set; } = string.Empty;

        public OrderConfirmedDomainEvent() 
        {
            ConfirmedAt = DateTime.UtcNow;
        }

        public OrderConfirmedDomainEvent(Entities.Order order, string confirmedBy, string? correlationId = null)
        {
            OrderId = order.Id;
            CustomerId = order.CustomerId;
            TotalAmount = Money.FromAmount(order.TotalAmount);
            ConfirmedAt = DateTime.UtcNow;
            ConfirmedBy = confirmedBy;
            CorrelationId = correlationId;

            Items = order.Items.Select(item => new OrderItemData
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = Money.FromAmount(item.UnitPrice),
                TotalPrice = Money.FromAmount(item.TotalPrice)
            }).ToList();
        }
    }

    /// <summary>
    /// Domain event published when an order is cancelled, requiring compensation
    /// across multiple domains including inventory release and payment refunds.
    /// </summary>
    /// <remarks>
    /// Order cancellation triggers important compensation workflows:
    /// 
    /// Compensation Requirements:
    /// - Inventory reservations or allocations must be released
    /// - Payment authorizations should be voided or refunds processed
    /// - Fulfillment processes must be halted or reversed
    /// - Customer communications should acknowledge cancellation
    /// 
    /// Cancellation Scenarios:
    /// - Customer-initiated cancellation before fulfillment
    /// - System-initiated cancellation due to inventory unavailability
    /// - Business rule violations requiring order cancellation
    /// - Fraud detection or security concerns
    /// 
    /// Timing Considerations:
    /// - Early cancellation (pending orders) requires minimal compensation
    /// - Late cancellation (confirmed orders) requires full compensation
    /// - Post-fulfillment scenarios should use returns process instead
    /// - Time-sensitive compensation to prevent customer impact
    /// 
    /// The event ensures proper cleanup and compensation across all
    /// systems involved in the order lifecycle management.
    /// </remarks>
    public class OrderCancelledDomainEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the cancelled order.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the customer whose order was cancelled.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Status the order was in before cancellation.
        /// Determines the scope of compensation required.
        /// </summary>
        public string PreviousStatus { get; set; } = string.Empty;

        /// <summary>
        /// Collection of items that were in the cancelled order.
        /// Required for inventory release and compensation processing.
        /// </summary>
        public List<OrderItemData> Items { get; set; } = new();

        /// <summary>
        /// UTC timestamp when the order was cancelled.
        /// </summary>
        public DateTime CancelledAt { get; set; }

        /// <summary>
        /// Identifier of the user or system that cancelled the order.
        /// </summary>
        public string CancelledBy { get; set; } = string.Empty;

        /// <summary>
        /// Optional reason for the cancellation for audit and customer service.
        /// </summary>
        public string? CancellationReason { get; set; }

        public OrderCancelledDomainEvent()
        {
            CancelledAt = DateTime.UtcNow;
        }

        public OrderCancelledDomainEvent(
            Entities.Order order, 
            string previousStatus, 
            string cancelledBy, 
            string? cancellationReason = null, 
            string? correlationId = null)
        {
            OrderId = order.Id;
            CustomerId = order.CustomerId;
            PreviousStatus = previousStatus;
            CancelledAt = DateTime.UtcNow;
            CancelledBy = cancelledBy;
            CancellationReason = cancellationReason;
            CorrelationId = correlationId;

            Items = order.Items.Select(item => new OrderItemData
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = Money.FromAmount(item.UnitPrice),
                TotalPrice = Money.FromAmount(item.TotalPrice)
            }).ToList();
        }
    }

    /// <summary>
    /// Data transfer object for order item information in domain events.
    /// Provides a serializable representation of order items that is independent
    /// of the domain entity structure and suitable for cross-boundary communication.
    /// </summary>
    /// <remarks>
    /// Event Data Design:
    /// - Simplified structure optimized for serialization
    /// - Self-contained data without entity relationships
    /// - Immutable after creation for event integrity
    /// - Rich enough for autonomous processing
    /// 
    /// Serialization Considerations:
    /// - Compatible with JSON, XML, and binary serialization
    /// - No circular references or complex object graphs
    /// - Platform-independent data types for cross-system compatibility
    /// - Versioning support through optional properties
    /// </remarks>
    public class OrderItemData
    {
        /// <summary>
        /// Identifier of the product in the order item.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Name of the product at the time of order.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of the product ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Price per unit of the product.
        /// </summary>
        public Money UnitPrice { get; set; }

        /// <summary>
        /// Total price for this line item (Quantity × UnitPrice).
        /// </summary>
        public Money TotalPrice { get; set; }
    }
}