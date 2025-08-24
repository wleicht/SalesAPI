using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Models
{
    /// <summary>
    /// Entity that maintains an audit trail of processed domain events to ensure idempotent message processing.
    /// Prevents duplicate execution of business operations when the same event is delivered multiple times
    /// by the distributed messaging infrastructure.
    /// </summary>
    /// <remarks>
    /// Idempotency is a critical requirement in distributed systems where message delivery guarantees
    /// can result in duplicate event processing. This entity provides:
    /// 
    /// Core Capabilities:
    /// - Duplicate Detection: Identifies previously processed events before business logic execution
    /// - Audit Trail: Complete history of event processing for compliance and debugging
    /// - Correlation Tracking: Links events across service boundaries for distributed tracing
    /// - Performance Optimization: Fast lookup capability for high-throughput scenarios
    /// 
    /// Implementation Strategy:
    /// - Database Constraints: Unique index on EventId ensures atomic duplicate detection
    /// - Transaction Integration: Created within the same transaction as business operations
    /// - Retention Policies: Support for automated cleanup of old processed event records
    /// - Query Optimization: Indexed fields for efficient duplicate checking
    /// 
    /// Business Impact:
    /// - Data Consistency: Prevents duplicate stock deductions and financial transactions
    /// - System Reliability: Enables safe retry mechanisms without side effects
    /// - Audit Compliance: Provides complete processing history for regulatory requirements
    /// - Operational Monitoring: Supports detection of message delivery issues
    /// 
    /// The ProcessedEvent pattern is essential for building reliable microservices that can
    /// handle the inherent challenges of distributed messaging systems while maintaining
    /// data integrity and business rule enforcement.
    /// </remarks>
    public class ProcessedEvent
    {
        /// <summary>
        /// Unique identifier for this processed event record in the database.
        /// Serves as the primary key for database operations and relationship management.
        /// </summary>
        /// <value>A GUID that uniquely identifies this database record</value>
        /// <remarks>
        /// Database Design:
        /// - Primary Key: Used as the clustered index for optimal query performance
        /// - Surrogate Key: Independent of business logic for stable relationships
        /// - Auto-Generation: Automatically assigned to ensure uniqueness
        /// - Immutable: Never changes after record creation
        /// 
        /// While EventId serves as the business key for idempotency checks,
        /// this Id provides database-level uniqueness and performance optimization
        /// for internal database operations and potential future relationships.
        /// </remarks>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The original unique identifier from the domain event that was processed.
        /// This is the critical field used for idempotency checking to prevent duplicate processing.
        /// </summary>
        /// <value>The EventId from the original DomainEvent instance</value>
        /// <remarks>
        /// Idempotency Implementation:
        /// - Unique Constraint: Database-level unique index prevents duplicate entries
        /// - Lookup Key: Primary field used for "already processed" checks
        /// - Business Key: Links directly to the original event's unique identifier
        /// - Immutable: Never changes after initial processing
        /// 
        /// Processing Flow:
        /// 1. Event handler receives domain event
        /// 2. Query ProcessedEvent table using EventId
        /// 3. If record exists, skip processing (idempotent behavior)
        /// 4. If not exists, process event and create ProcessedEvent record
        /// 5. Both operations occur within same database transaction
        /// 
        /// The EventId serves as the bridge between the distributed messaging system
        /// and the local idempotency tracking, ensuring that each unique business
        /// event is processed exactly once regardless of delivery duplicates.
        /// </remarks>
        [Required]
        public required Guid EventId { get; set; }

        /// <summary>
        /// The fully qualified type name of the domain event that was processed.
        /// Enables event type-specific analysis, debugging, and operational monitoring.
        /// </summary>
        /// <value>String representation of the event type (e.g., "OrderConfirmedEvent", "StockDebitedEvent")</value>
        /// <remarks>
        /// Type Information Benefits:
        /// - Debugging Support: Quickly identify what type of event was processed
        /// - Analytics Capability: Aggregate processing metrics by event type
        /// - Monitoring Integration: Alert on unusual patterns or error rates by type
        /// - Maintenance Operations: Selective cleanup or replay based on event types
        /// 
        /// Naming Convention:
        /// - Simple Name: Use the class name without namespace for readability
        /// - Consistent Format: Maintain consistent naming across all event types
        /// - Version Independence: Type name should remain stable across schema versions
        /// - Human Readable: Optimized for debugging and operational visibility
        /// 
        /// Operational Uses:
        /// - Performance monitoring per event type
        /// - Error rate analysis by business operation
        /// - Capacity planning based on event volume patterns
        /// - Troubleshooting specific business process issues
        /// </remarks>
        [Required]
        [MaxLength(200)]
        public required string EventType { get; set; }

        /// <summary>
        /// The business order identifier associated with this event processing operation.
        /// Enables order-specific troubleshooting, customer service, and business analytics.
        /// </summary>
        /// <value>Order GUID when applicable, or null for non-order-related events</value>
        /// <remarks>
        /// Business Context Benefits:
        /// - Customer Service: Link processing records to specific customer orders
        /// - Troubleshooting: Trace all processing steps for problematic orders
        /// - Analytics: Order-level processing time and success rate analysis
        /// - Audit Trail: Complete order fulfillment history for compliance
        /// 
        /// Optional Nature:
        /// - Order Events: Always populated for order-related events
        /// - System Events: May be null for infrastructure or non-order events
        /// - Future Events: Extensible for other business entity types
        /// - Migration Support: Nullable to support historical data without order context
        /// 
        /// Query Patterns:
        /// - Order History: Find all events processed for a specific order
        /// - Processing Timeline: Chronological view of order fulfillment steps
        /// - Error Investigation: Identify which orders experienced processing issues
        /// - Performance Analysis: Order complexity vs. processing time correlation
        /// 
        /// The OrderId provides crucial business context that bridges the technical
        /// event processing infrastructure with customer-facing business operations.
        /// </remarks>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// UTC timestamp indicating when this event was successfully processed and committed.
        /// Provides temporal context for audit trails, SLA monitoring, and performance analysis.
        /// </summary>
        /// <value>UTC DateTime when the event processing was completed</value>
        /// <remarks>
        /// Temporal Analysis Capabilities:
        /// - Processing Duration: Calculate time from event occurrence to completion
        /// - SLA Monitoring: Track processing times against service level agreements
        /// - Peak Load Analysis: Identify processing patterns and capacity requirements
        /// - Historical Trending: Long-term analysis of system performance improvements
        /// 
        /// Audit and Compliance:
        /// - Regulatory Reporting: Timestamp evidence for audit and compliance requirements
        /// - Data Retention: Support automated cleanup of old processed event records
        /// - Forensic Analysis: Precise timing information for security and error investigations
        /// - Business Intelligence: Processing time patterns for business process optimization
        /// 
        /// Operational Monitoring:
        /// - Real-time Dashboards: Current processing lag and throughput metrics
        /// - Alerting Systems: Trigger alerts when processing times exceed thresholds
        /// - Capacity Planning: Historical data for infrastructure scaling decisions
        /// - Performance Optimization: Identify bottlenecks and optimization opportunities
        /// 
        /// Time Zone Considerations:
        /// - UTC Storage: Eliminates time zone ambiguity for global deployments
        /// - Consistent Ordering: Enables accurate chronological sequencing
        /// - Cross-Service Correlation: Compatible with other service timestamp formats
        /// - Daylight Saving Time: Immune to local time zone changes and complications
        /// </remarks>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Correlation identifier that links this processing record to the originating request chain.
        /// Enables end-to-end distributed tracing across all services involved in the business operation.
        /// </summary>
        /// <value>Correlation ID string from the original request or null if not available</value>
        /// <remarks>
        /// Distributed Tracing Benefits:
        /// - End-to-End Visibility: Track complete customer request journeys across services
        /// - Error Investigation: Correlate failures across multiple service boundaries
        /// - Performance Analysis: Measure total customer request processing times
        /// - Business Process Monitoring: Understand complex multi-step workflows
        /// 
        /// Correlation Propagation:
        /// - HTTP Headers: Originating from API Gateway or client applications
        /// - Message Headers: Carried through event publishing and consumption
        /// - Database Records: Persisted at each processing step for historical analysis
        /// - Log Aggregation: Enables correlation across distributed log streams
        /// 
        /// Monitoring Integration:
        /// - APM Tools: Integration with Application Performance Monitoring solutions
        /// - Log Analysis: Structured logging with correlation ID for efficient querying
        /// - Metrics Collection: Business-level metrics aggregated by correlation ID
        /// - Dashboard Visualization: Request flow visualization and bottleneck identification
        /// 
        /// Troubleshooting Workflows:
        /// - Customer Issues: Start with customer complaint and trace complete processing
        /// - System Issues: Identify impact scope by correlating related processing records
        /// - Performance Problems: Analyze end-to-end timing for slow request patterns
        /// - Data Consistency: Verify complete processing across all related services
        /// 
        /// The CorrelationId transforms isolated processing records into a comprehensive
        /// view of distributed business operations, essential for modern microservices
        /// observability and operational excellence.
        /// </remarks>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Optional supplementary information about the event processing outcome, errors, or context.
        /// Provides additional diagnostic information for troubleshooting and business intelligence purposes.
        /// </summary>
        /// <value>Human-readable processing details or null if not applicable</value>
        /// <remarks>
        /// Content Guidelines:
        /// - Success Details: Summary of processing results (e.g., "3 items processed, stock reduced by 15 units")
        /// - Error Information: Specific error descriptions and context for failed processing
        /// - Business Context: Relevant business information that affected processing
        /// - Performance Data: Processing statistics or resource utilization information
        /// 
        /// Use Cases:
        /// - Debugging Support: Additional context for understanding processing behavior
        /// - Business Analytics: Rich data for business intelligence and reporting
        /// - Customer Service: Detailed information for customer inquiry resolution
        /// - Operational Monitoring: Extended metrics for system health assessment
        /// 
        /// Content Examples:
        /// - "Successfully debited 5 units from Product ABC123, new stock: 45"
        /// - "Partial processing: 2 of 3 items processed, insufficient stock for Product XYZ789"
        /// - "Processing completed in 250ms, database transaction committed"
        /// - "Error: External service timeout during inventory validation"
        /// 
        /// Storage Considerations:
        /// - Length Limits: 1000 characters maximum to balance detail with storage efficiency
        /// - Structured Format: Consider JSON for complex details requiring parsing
        /// - Sensitive Data: Avoid storing personally identifiable or financial information
        /// - Performance Impact: Optional field that doesn't affect critical processing paths
        /// 
        /// The ProcessingDetails field enhances the operational value of the audit trail
        /// by providing context that goes beyond simple success/failure indicators,
        /// supporting both technical troubleshooting and business analysis requirements.
        /// </remarks>
        [MaxLength(1000)]
        public string? ProcessingDetails { get; set; }
    }
}