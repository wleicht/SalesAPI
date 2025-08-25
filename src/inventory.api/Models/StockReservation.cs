using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Models
{
    /// <summary>
    /// Represents the possible states of a stock reservation in the order fulfillment lifecycle.
    /// Defines the progression from initial reservation through final disposition.
    /// </summary>
    /// <remarks>
    /// State Transition Flow:
    /// Reserved ? Debited (successful order completion)
    /// Reserved ? Released (order cancellation or failure)
    /// 
    /// Terminal states (Debited, Released) cannot transition to other states,
    /// ensuring audit trail integrity and preventing state corruption.
    /// </remarks>
    public enum ReservationStatus
    {
        /// <summary>
        /// Stock has been reserved for an order but not yet committed.
        /// This is the initial state when stock is allocated but payment/confirmation is pending.
        /// Stock is unavailable to other orders but can still be released if the order fails.
        /// </summary>
        Reserved = 1,

        /// <summary>
        /// Reserved stock has been permanently debited from inventory due to successful order completion.
        /// This is a terminal state indicating the reservation has been converted to an actual sale.
        /// Stock is permanently reduced and cannot be returned to available inventory.
        /// </summary>
        Debited = 2,

        /// <summary>
        /// Reserved stock has been released back to available inventory due to order cancellation or failure.
        /// This is a terminal state indicating the reservation was unsuccessful and stock is available again.
        /// Occurs when payment fails, order is cancelled, or business rules prevent order completion.
        /// </summary>
        Released = 3
    }

    /// <summary>
    /// Entity representing a temporary stock reservation that prevents race conditions during order processing.
    /// Implements the Saga pattern for distributed transaction management across Sales and Inventory services.
    /// Ensures data consistency and prevents overselling in high-concurrency scenarios.
    /// </summary>
    /// <remarks>
    /// Business Purpose:
    /// The StockReservation entity implements a critical reliability pattern for e-commerce systems
    /// by temporarily allocating inventory during order processing. This prevents the classic
    /// "overselling" problem where multiple customers can simultaneously purchase the last item.
    /// 
    /// Saga Pattern Implementation:
    /// - Reservation Phase: Stock is reserved synchronously during order creation
    /// - Compensation Phase: Stock is released if order processing fails
    /// - Completion Phase: Stock is debited when order is successfully confirmed
    /// 
    /// Concurrency Control:
    /// - Atomic Operations: Reservations are created atomically with stock checks
    /// - Isolation: Reserved stock is unavailable to other concurrent transactions
    /// - Consistency: Total reserved + available stock always equals physical stock
    /// - Durability: Reservations survive system restarts and failures
    /// 
    /// Integration with Event-Driven Architecture:
    /// - HTTP Sync: Initial reservation creation for immediate feedback
    /// - Event Async: Order confirmation/cancellation processing via events
    /// - Idempotency: Event processing uses reservation IDs for duplicate detection
    /// - Audit Trail: Complete history of stock allocation decisions
    /// 
    /// The entity supports complex business scenarios including partial reservations,
    /// timeout-based releases, and integration with external payment systems.
    /// </remarks>
    public class StockReservation
    {
        /// <summary>
        /// Unique identifier for this stock reservation record.
        /// Serves as the primary key and enables precise tracking of individual reservations.
        /// </summary>
        /// <value>A GUID that uniquely identifies this reservation across the system</value>
        /// <remarks>
        /// Database Design:
        /// - Primary Key: Clustered index for optimal query performance
        /// - Business Key: Used for event correlation and API operations
        /// - Immutable: Never changes after reservation creation
        /// - Audit Trail: Links to event processing records for complete history
        /// 
        /// The Id serves as the definitive reference for all reservation operations
        /// and provides the correlation key between synchronous API calls and
        /// asynchronous event processing workflows.
        /// </remarks>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Identifier of the order for which this stock reservation was created.
        /// Links the reservation to the specific business transaction requiring inventory allocation.
        /// </summary>
        /// <value>Order GUID from the Sales service that triggered this reservation</value>
        /// <remarks>
        /// Cross-Service Correlation:
        /// - Sales Integration: Links back to Order entity in Sales database
        /// - Event Processing: Used for event routing and correlation
        /// - Customer Service: Enables order-specific inventory inquiry support
        /// - Analytics: Supports order-to-inventory allocation analysis
        /// 
        /// Business Context:
        /// - Order Lifecycle: Tracks inventory allocation throughout order processing
        /// - Customer Experience: Ensures reserved items remain available during checkout
        /// - Financial Reconciliation: Links inventory movements to revenue transactions
        /// - Audit Compliance: Provides complete order fulfillment audit trail
        /// 
        /// Multiple reservations can exist for a single order when the order
        /// contains multiple products, enabling granular inventory management
        /// and partial order fulfillment scenarios.
        /// </remarks>
        [Required]
        public required Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the product for which stock is being reserved.
        /// References the Product entity and enables product-specific inventory management.
        /// </summary>
        /// <value>Product GUID that identifies the specific inventory item being reserved</value>
        /// <remarks>
        /// Inventory Management:
        /// - Product Linkage: Direct reference to Product entity for stock validation
        /// - Concurrency Control: Enables product-level locking for race condition prevention
        /// - Availability Calculation: Used for real-time stock availability queries
        /// - Reporting: Supports product-specific reservation and allocation reporting
        /// 
        /// Business Operations:
        /// - Stock Validation: Ensures reservations don't exceed available inventory
        /// - Product Queries: Enables efficient product availability lookups
        /// - Demand Planning: Provides reservation data for inventory forecasting
        /// - Supplier Integration: Links reservations to procurement and replenishment
        /// 
        /// The ProductId enables precise inventory tracking at the SKU level
        /// and supports complex scenarios like variant management and
        /// multi-location inventory allocation.
        /// </remarks>
        [Required]
        public required Guid ProductId { get; set; }

        /// <summary>
        /// Human-readable name of the product at the time of reservation creation.
        /// Provides contextual information for audit trails and customer service operations.
        /// </summary>
        /// <value>Product name snapshot from reservation time</value>
        /// <remarks>
        /// Data Integrity:
        /// - Snapshot Approach: Captures product name at reservation time for historical accuracy
        /// - Audit Trail: Maintains readable audit records even if product names change
        /// - Customer Service: Enables clear communication about reserved items
        /// - Reporting: Provides human-readable reservation reports without complex joins
        /// 
        /// Operational Benefits:
        /// - Debugging Support: Clear identification of reserved products in logs
        /// - Performance: Avoids joins for display purposes in high-volume scenarios
        /// - Data Consistency: Preserves historical context regardless of product changes
        /// - User Interface: Direct display capability without additional lookups
        /// </remarks>
        [Required]
        [MaxLength(200)]
        public required string ProductName { get; set; }

        /// <summary>
        /// Quantity of product units reserved for the associated order.
        /// Represents the exact number of items temporarily allocated from available inventory.
        /// </summary>
        /// <value>Positive integer representing the number of units reserved</value>
        /// <remarks>
        /// Inventory Allocation:
        /// - Unit Precision: Exact quantity reserved, matching order requirements
        /// - Availability Impact: This quantity is subtracted from available-to-promise inventory
        /// - Validation: Must not exceed available stock at reservation time
        /// - Aggregation: Multiple reservations for same product are additive
        /// 
        /// Business Rules:
        /// - Minimum Value: Must be at least 1 (cannot reserve zero or negative quantities)
        /// - Maximum Value: Cannot exceed available stock at reservation time
        /// - Precision: Integer quantities only (no fractional units supported)
        /// - Immutability: Quantity cannot be modified after reservation creation
        /// 
        /// Concurrency Considerations:
        /// - Atomic Allocation: Quantity reserved atomically with availability check
        /// - Race Prevention: Prevents overselling in high-concurrency scenarios
        /// - Stock Consistency: Reserved quantity always reflects committed allocation
        /// - Audit Accuracy: Provides precise inventory movement history
        /// </remarks>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public required int Quantity { get; set; }

        /// <summary>
        /// Current status of this stock reservation in the order fulfillment lifecycle.
        /// Tracks the progression from initial allocation through final disposition.
        /// </summary>
        /// <value>ReservationStatus enum value indicating current reservation state</value>
        /// <remarks>
        /// Status Lifecycle:
        /// - Reserved: Initial state when stock is allocated pending order confirmation
        /// - Debited: Final state when order completes and stock is permanently consumed
        /// - Released: Final state when order fails and stock returns to available pool
        /// 
        /// State Transitions:
        /// - Reserved ? Debited: OrderConfirmedEvent processed successfully
        /// - Reserved ? Released: OrderCancelledEvent processed or timeout reached
        /// - Terminal States: Debited and Released states cannot transition further
        /// 
        /// Business Logic:
        /// - Inventory Calculation: Only 'Reserved' status affects available stock
        /// - Event Processing: Status drives event handler logic and business rules
        /// - Audit Trail: Status changes provide complete reservation history
        /// - Monitoring: Status distribution indicates system health and performance
        /// 
        /// The status field is the primary control mechanism for reservation
        /// lifecycle management and enables sophisticated inventory allocation
        /// strategies and business rule enforcement.
        /// </remarks>
        [Required]
        public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

        /// <summary>
        /// UTC timestamp when this stock reservation was initially created.
        /// Provides temporal context for reservation lifecycle and timeout management.
        /// </summary>
        /// <value>UTC DateTime of reservation creation</value>
        /// <remarks>
        /// Temporal Management:
        /// - Lifecycle Tracking: Enables calculation of reservation duration and aging
        /// - Timeout Processing: Supports automatic release of expired reservations
        /// - SLA Monitoring: Tracks order processing time from reservation to completion
        /// - Analytics: Historical data for inventory allocation pattern analysis
        /// 
        /// Business Applications:
        /// - Customer Experience: Prevents indefinite stock holds that impact availability
        /// - Operational Efficiency: Identifies slow-processing orders requiring attention
        /// - Inventory Optimization: Data for improving stock allocation algorithms
        /// - Compliance: Audit trail timestamps for regulatory and business requirements
        /// 
        /// Timeout Scenarios:
        /// - Payment Processing: Limited time for customer to complete payment
        /// - System Integration: Reasonable timeouts for external service dependencies
        /// - Resource Management: Prevents resource leaks from abandoned orders
        /// - Business Rules: Configurable timeout policies based on business requirements
        /// 
        /// All timestamps are stored in UTC to ensure consistency across global
        /// deployments and eliminate time zone complexity in business logic.
        /// </remarks>
        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when this reservation was processed to its final state (Debited or Released).
        /// Null while reservation remains in Reserved status, set when terminal state is reached.
        /// </summary>
        /// <value>UTC DateTime of final processing or null if still in Reserved status</value>
        /// <remarks>
        /// Lifecycle Completion:
        /// - Processing Duration: Enables calculation of total reservation-to-completion time
        /// - Audit Trail: Precise timestamp of final business decision (commit or rollback)
        /// - SLA Compliance: Measurement data for order processing performance targets
        /// - Analytics: Historical data for process optimization and capacity planning
        /// 
        /// Business Intelligence:
        /// - Conversion Rates: Analysis of reservation-to-order conversion efficiency
        /// - Processing Patterns: Identification of peak processing times and bottlenecks
        /// - Customer Behavior: Understanding of checkout abandonment and completion patterns
        /// - Operational Metrics: Data for continuous improvement of fulfillment processes
        /// 
        /// Integration Points:
        /// - Event Processing: Set when OrderConfirmedEvent or OrderCancelledEvent is processed
        /// - Monitoring: Used for real-time dashboards and alerting systems
        /// - Reporting: Key metric for business reporting and management dashboards
        /// - Cleanup: Enables identification of completed reservations for archival
        /// 
        /// Null Semantics:
        /// - Null Value: Indicates reservation is still active (Reserved status)
        /// - Non-Null Value: Indicates reservation has reached terminal state
        /// - Immutability: Once set, this timestamp should never be modified
        /// - Consistency: Must be set whenever Status transitions to Debited or Released
        /// </remarks>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Correlation identifier linking this reservation to the originating request chain.
        /// Enables end-to-end distributed tracing across all services involved in order processing.
        /// </summary>
        /// <value>Correlation ID string from the original request or null if not available</value>
        /// <remarks>
        /// Distributed Tracing Benefits:
        /// - End-to-End Visibility: Track customer requests from initial order through inventory allocation
        /// - Cross-Service Correlation: Link reservation operations to Sales API order creation
        /// - Error Investigation: Correlate failures across service boundaries for root cause analysis
        /// - Performance Analysis: Measure total customer request processing times including inventory
        /// 
        /// Operational Excellence:
        /// - Debugging Support: Quickly identify all operations related to a customer issue
        /// - Monitoring Integration: APM tools can construct complete request flow visualizations
        /// - Log Aggregation: Structured logging with correlation enables efficient troubleshooting
        /// - Business Intelligence: Customer journey analysis and process optimization insights
        /// 
        /// Integration Patterns:
        /// - API Gateway: Originating correlation IDs from customer-facing requests
        /// - Event Processing: Propagated through OrderConfirmedEvent and OrderCancelledEvent
        /// - Database Operations: Persisted for historical analysis and audit requirements
        /// - External Services: Included in all downstream service communications
        /// 
        /// The CorrelationId transforms isolated reservation records into part of
        /// a comprehensive distributed transaction view, essential for modern
        /// microservices observability and customer experience optimization.
        /// </remarks>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }
    }
}