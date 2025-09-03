using BuildingBlocks.Events.Domain;
using System;

namespace BuildingBlocks.Events.Domain
{
    /// <summary>
    /// Abstract base class for all domain events in the microservices architecture.
    /// Provides essential infrastructure for event identification, temporal tracking, correlation, and versioning.
    /// Ensures consistent event metadata across all business domain events in the system.
    /// </summary>
    /// <remarks>
    /// The DomainEvent base class establishes the foundational infrastructure for event-driven architecture
    /// by providing standardized metadata that supports:
    /// 
    /// Core Capabilities:
    /// - Unique event identification for deduplication and idempotency
    /// - Temporal ordering and chronological event analysis
    /// - Cross-service correlation and distributed tracing
    /// - Schema evolution and backward compatibility management
    /// 
    /// Design Principles:
    /// - Immutability: Core properties are protected to prevent modification after creation
    /// - Self-describing: Events contain all necessary metadata for processing
    /// - Traceable: Full audit trail capabilities through correlation identifiers
    /// - Versionable: Schema evolution support for long-term system maintainability
    /// 
    /// Integration Patterns:
    /// All concrete domain events inherit from this base class, ensuring consistent
    /// behavior across the event-driven messaging infrastructure. This standardization
    /// enables reliable message processing, correlation tracking, and operational monitoring.
    /// 
    /// Best Practices:
    /// - Always inherit from DomainEvent for business events
    /// - Use correlation IDs for end-to-end request tracking
    /// - Increment version numbers when making breaking schema changes
    /// - Treat events as immutable data structures after creation
    /// </remarks>
    public abstract class DomainEvent : IDomainEvent
    {
        /// <summary>
        /// Globally unique identifier for this specific event instance.
        /// Enables event deduplication, idempotency checks, and precise event tracking across the distributed system.
        /// </summary>
        /// <value>A GUID that uniquely identifies this event instance across all services and time</value>
        /// <remarks>
        /// Critical Uses:
        /// - Idempotency: Prevents duplicate processing of the same event
        /// - Event Sourcing: Enables precise event replay and audit trails
        /// - Debugging: Allows exact event identification in logs and monitoring
        /// - Dead Letter Handling: Supports detailed error investigation and recovery
        /// 
        /// Implementation Notes:
        /// - Generated automatically using Guid.NewGuid() for guaranteed uniqueness
        /// - Protected setter prevents accidental modification after creation
        /// - Remains constant throughout the event's lifecycle
        /// - Used by message infrastructure for deduplication guarantees
        /// 
        /// The EventId is the primary key for event tracking and should never be
        /// modified after the event instance is created.
        /// </remarks>
        public Guid EventId { get; protected set; } = Guid.NewGuid();

        /// <summary>
        /// UTC timestamp indicating when this domain event occurred in the business context.
        /// Provides temporal context for event ordering, analytics, and audit trail reconstruction.
        /// </summary>
        /// <value>UTC DateTime representing when the business event occurred</value>
        /// <remarks>
        /// Temporal Significance:
        /// - Business Time: Represents when the business event actually happened
        /// - Event Ordering: Enables chronological reconstruction of business processes
        /// - Analytics: Supports time-based business intelligence and reporting
        /// - Audit Compliance: Provides regulatory timestamp requirements
        /// - SLA Monitoring: Enables processing time analysis and optimization
        /// 
        /// Technical Considerations:
        /// - Always stored in UTC to eliminate time zone ambiguity
        /// - Set at event creation time, not processing time
        /// - Protected setter maintains timestamp immutability
        /// - Differs from message broker timestamps (transport vs. business time)
        /// 
        /// The OccurredAt timestamp represents business time and should not be
        /// confused with infrastructure timestamps like message send/receive times.
        /// </remarks>
        public DateTime OccurredAt { get; protected set; } = DateTime.UtcNow;

        /// <summary>
        /// Correlation identifier for tracking related events and operations across service boundaries.
        /// Enables end-to-end distributed tracing and complete request flow visibility in microservices architecture.
        /// </summary>
        /// <value>String identifier linking related events across the distributed system, or null if not applicable</value>
        /// <remarks>
        /// Distributed Tracing Benefits:
        /// - Request Flow: Tracks complete user requests across multiple services
        /// - Error Investigation: Links related events when troubleshooting failures
        /// - Performance Analysis: Measures end-to-end processing times
        /// - Business Process Tracking: Follows complex workflows across service boundaries
        /// - Audit Trails: Maintains complete operation histories for compliance
        /// 
        /// Implementation Patterns:
        /// - HTTP Requests: Propagated from initial API request headers
        /// - Message Processing: Carried forward through event chains
        /// - Batch Operations: Groups related batch items under common correlation
        /// - Manual Assignment: Set explicitly for business process tracking
        /// 
        /// Best Practices:
        /// - Propagate correlation IDs through entire request chains
        /// - Use consistent format across all services (e.g., GUIDs or structured strings)
        /// - Include in all structured logs for efficient querying
        /// - Consider hierarchical correlation for complex nested operations
        /// 
        /// Optional Nature:
        /// While correlation IDs are highly recommended for production systems,
        /// they remain optional to support legacy integrations and simple scenarios.
        /// </remarks>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Schema version number for this event type, supporting backward compatibility and schema evolution.
        /// Enables safe deployment of event schema changes without breaking existing consumers.
        /// </summary>
        /// <value>Integer version number, defaults to 1 for initial schema version</value>
        /// <remarks>
        /// Schema Evolution Strategy:
        /// - Additive Changes: New optional properties can be added without version increment
        /// - Breaking Changes: Require version increment and compatibility handling
        /// - Consumer Compatibility: Allows consumers to handle multiple schema versions
        /// - Migration Support: Enables gradual rollout of schema changes across services
        /// 
        /// Versioning Guidelines:
        /// - Start with version 1 for all new event types
        /// - Increment only for breaking changes (removed/renamed fields, type changes)
        /// - Document version changes in event type documentation
        /// - Maintain backward compatibility for at least one version
        /// 
        /// Implementation Approach:
        /// - Virtual property allows concrete events to override default version
        /// - Message processors can implement version-specific handling logic
        /// - Event stores can maintain multiple schema versions simultaneously
        /// - Migration tooling can leverage version information for data upgrades
        /// 
        /// Production Considerations:
        /// In production environments, version mismatches should be monitored
        /// and resolved promptly to prevent data processing issues and ensure
        /// system reliability across service deployments.
        /// </remarks>
        public virtual int Version => 1;
    }
}