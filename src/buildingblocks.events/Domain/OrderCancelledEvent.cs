namespace BuildingBlocks.Events.Domain
{
    /// <summary>
    /// Domain event published when an order has been cancelled and requires stock reservation release.
    /// This event triggers automatic release of reserved inventory back to available stock in the Inventory service.
    /// Implements the compensation pattern for distributed transaction management across microservices.
    /// </summary>
    /// <remarks>
    /// The OrderCancelledEvent represents a critical compensation mechanism in the distributed order
    /// processing workflow. When published, it indicates that:
    /// 
    /// 1. The order processing has failed after stock reservation
    /// 2. Payment processing was unsuccessful or declined
    /// 3. Business rule validation prevented order completion
    /// 4. Customer explicitly cancelled the order before confirmation
    /// 5. System timeout or error occurred during processing
    /// 
    /// Compensation Pattern Implementation:
    /// This event implements the Saga pattern's compensation phase, ensuring that any stock
    /// reservations made during order creation are properly released when the order cannot
    /// be completed. This maintains inventory accuracy and prevents permanent stock locks.
    /// 
    /// Event Processing Guarantees:
    /// - Events are processed exactly once (idempotency)
    /// - Stock reservations are released within database transactions
    /// - Failed processing triggers automatic retry mechanisms
    /// - All operations are fully auditable with correlation tracking
    /// 
    /// Integration Pattern:
    /// This event follows the Event-Driven Architecture pattern where services communicate
    /// asynchronously through domain events. The compensation pattern ensures system
    /// consistency even when distributed operations fail partially.
    /// 
    /// Business Impact:
    /// - Inventory Accuracy: Prevents permanent loss of saleable inventory
    /// - Customer Experience: Ensures stock returns to available pool quickly
    /// - System Reliability: Enables robust error recovery and fault tolerance
    /// - Operational Efficiency: Reduces manual intervention for failed orders
    /// </remarks>
    public class OrderCancelledEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the cancelled order.
        /// References the Order entity in the Sales database and enables correlation with inventory operations.
        /// </summary>
        /// <value>A GUID that uniquely identifies the cancelled order across all services</value>
        /// <remarks>
        /// This identifier enables:
        /// - Correlation with existing stock reservations for release processing
        /// - Audit trail linking cancellation events to original order creation
        /// - Customer service support for cancelled order inquiries
        /// - Analytics and reporting on order cancellation patterns and rates
        /// 
        /// The OrderId serves as the primary correlation key between the Sales service's
        /// order cancellation decision and the Inventory service's reservation release processing.
        /// </remarks>
        public required Guid OrderId { get; set; }

        /// <summary>
        /// Unique identifier of the customer who placed the cancelled order.
        /// Enables customer-specific processing and provides context for cancellation handling.
        /// </summary>
        /// <value>A GUID that uniquely identifies the customer in the system</value>
        /// <remarks>
        /// Customer Context Benefits:
        /// - Customer Service: Enables customer-specific cancellation tracking and support
        /// - Analytics: Supports customer behavior analysis and retention strategies
        /// - Business Intelligence: Data for understanding cancellation patterns by customer
        /// - Personalization: Future enhancement potential for customer-specific handling
        /// 
        /// While not directly used for inventory processing, this information provides
        /// valuable business context for comprehensive order lifecycle management
        /// and customer experience optimization.
        /// </remarks>
        public required Guid CustomerId { get; set; }

        /// <summary>
        /// Total monetary amount of the cancelled order for financial tracking and reconciliation.
        /// Provides context for the business impact of the cancellation.
        /// </summary>
        /// <value>The total order amount in the system's base currency</value>
        /// <remarks>
        /// Financial Context:
        /// - Revenue Impact: Quantifies the financial impact of order cancellation
        /// - Reporting: Supports financial reporting and cancellation cost analysis
        /// - Business Intelligence: Data for understanding revenue loss patterns
        /// - Compensation: Context for potential customer compensation decisions
        /// 
        /// This amount represents the value of inventory that will be returned to
        /// available stock and provides important context for business impact
        /// assessment and trend analysis.
        /// </remarks>
        public required decimal TotalAmount { get; set; }

        /// <summary>
        /// Collection of items in the cancelled order that require stock reservation release.
        /// Each item will trigger a corresponding reservation release operation in the Inventory service.
        /// </summary>
        /// <value>A collection of OrderItemEvent objects representing the cancelled order contents</value>
        /// <remarks>
        /// Processing Logic:
        /// - Each item is processed independently for maximum granularity
        /// - Failed item processing doesn't affect other items in the same cancellation
        /// - All item operations are performed within a single database transaction
        /// - Reservation status validation ensures only active reservations are released
        /// 
        /// Compensation Behavior:
        /// - Stock reservations are released back to available inventory
        /// - Reserved quantities become available for new orders immediately
        /// - Audit trail maintains complete history of reservation lifecycle
        /// - Idempotency ensures safe reprocessing of duplicate cancellation events
        /// 
        /// The collection provides the detailed inventory context needed for precise
        /// stock reservation release operations and comprehensive audit trail maintenance.
        /// </remarks>
        public required ICollection<OrderItemEvent> Items { get; set; }

        /// <summary>
        /// Reason code or description explaining why the order was cancelled.
        /// Provides business context for cancellation analysis and process improvement.
        /// </summary>
        /// <value>Human-readable cancellation reason for analysis and reporting</value>
        /// <remarks>
        /// Cancellation Reason Categories:
        /// - Payment Failed: Credit card declined, insufficient funds, payment timeout
        /// - Customer Cancelled: Explicit customer cancellation during checkout
        /// - System Error: Technical failures preventing order completion
        /// - Business Rules: Validation failures, fraud detection, compliance issues
        /// - Inventory Issues: Stock unavailable despite reservation (rare edge case)
        /// 
        /// Business Intelligence Applications:
        /// - Process Improvement: Identify common failure points for optimization
        /// - Customer Experience: Understand pain points in the order process
        /// - System Reliability: Track technical failure rates and patterns
        /// - Fraud Detection: Monitor suspicious cancellation patterns
        /// 
        /// The cancellation reason enables targeted improvements to the order
        /// processing workflow and provides valuable insight into system
        /// performance and customer experience quality.
        /// </remarks>
        public required string CancellationReason { get; set; }

        /// <summary>
        /// Current status of the order at the time of cancellation event publication.
        /// Provides context about the order state when cancellation occurred.
        /// </summary>
        /// <value>A string representing the order status, typically "Cancelled"</value>
        /// <remarks>
        /// Expected Status Values:
        /// - "Cancelled": Order was explicitly cancelled by customer or system
        /// - "PaymentFailed": Order failed due to payment processing issues
        /// - "ValidationFailed": Order failed business rule validation
        /// - "SystemError": Order failed due to technical issues
        /// 
        /// This status provides important context for understanding the specific
        /// type of cancellation and enables status-specific processing logic
        /// and business rule enforcement in the inventory system.
        /// </remarks>
        public required string Status { get; set; }

        /// <summary>
        /// Timestamp when the original order was created by the customer.
        /// Provides temporal context for cancellation analysis and customer experience metrics.
        /// </summary>
        /// <value>UTC timestamp of the original order creation</value>
        /// <remarks>
        /// Analytics and Metrics:
        /// - Time-to-Cancellation: Measure how quickly orders fail after creation
        /// - Customer Behavior: Understand checkout abandonment timing patterns
        /// - Process Efficiency: Identify slow processing steps that increase cancellation risk
        /// - Business Intelligence: Historical data for conversion optimization
        /// 
        /// Customer Experience Insights:
        /// - Checkout Flow: Identify points where customers abandon orders
        /// - Payment Processing: Measure payment success vs. failure timing
        /// - System Performance: Correlate system response times with cancellation rates
        /// - Seasonal Patterns: Understand temporal trends in order completion rates
        /// 
        /// All timestamps are stored in UTC for consistency across time zones
        /// and enable accurate cross-system temporal analysis and reporting.
        /// </remarks>
        public required DateTime OrderCreatedAt { get; set; }

        /// <summary>
        /// Timestamp when the order cancellation occurred.
        /// Provides precise timing for cancellation analysis and audit requirements.
        /// </summary>
        /// <value>UTC timestamp when the cancellation was processed</value>
        /// <remarks>
        /// Operational Metrics:
        /// - Cancellation Processing Time: Measure system responsiveness to cancellation requests
        /// - Audit Compliance: Precise timestamps for regulatory and business audit requirements
        /// - SLA Monitoring: Track cancellation processing against service level agreements
        /// - Business Intelligence: Temporal patterns in cancellation events
        /// 
        /// This timestamp enables calculation of order lifecycle durations and
        /// provides critical audit trail information for business and regulatory
        /// compliance requirements.
        /// </remarks>
        public required DateTime CancelledAt { get; set; }
    }
}