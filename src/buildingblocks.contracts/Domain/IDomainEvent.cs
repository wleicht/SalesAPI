using System;

namespace BuildingBlocks.Events.Domain
{
    /// <summary>
    /// Defines the essential contract for all domain events in the event-driven architecture.
    /// Provides the minimal interface required for event identification, temporal tracking,
    /// and correlation support across distributed microservices implementations.
    /// </summary>
    /// <remarks>
    /// The IDomainEvent interface establishes the foundation for event-driven communication
    /// by defining the essential metadata required for reliable event processing:
    /// 
    /// Core Requirements:
    /// - Unique event identification for deduplication and tracking
    /// - Temporal information for ordering and time-based processing
    /// - Correlation support for distributed request tracing
    /// - Version information for schema evolution management
    /// 
    /// Design Principles:
    /// - Minimal interface for maximum implementation flexibility
    /// - Immutable properties for event integrity and consistency
    /// - Self-describing events with complete metadata
    /// - Technology-agnostic design for messaging provider independence
    /// 
    /// Integration Benefits:
    /// - Consistent event handling across all messaging infrastructure
    /// - Automatic serialization and deserialization support
    /// - Built-in correlation and tracing capabilities
    /// - Schema evolution and backward compatibility support
    /// 
    /// Implementation Guidelines:
    /// - All business events should implement this interface
    /// - Use DomainEvent base class for default implementation
    /// - Ensure immutability of event data after creation
    /// - Include sufficient context for event processing
    /// 
    /// The interface enables reliable, traceable, and evolvable event-driven
    /// communication patterns across distributed system boundaries.
    /// </remarks>
    public interface IDomainEvent
    {
        /// <summary>
        /// Globally unique identifier for this specific event instance.
        /// Essential for event deduplication, idempotency checks, and precise event tracking
        /// across distributed systems and messaging infrastructure.
        /// </summary>
        /// <value>A unique identifier that distinguishes this event from all others</value>
        /// <remarks>
        /// Event Identification Requirements:
        /// - Must be globally unique across all services and time
        /// - Should be generated using cryptographically secure methods
        /// - Must remain constant throughout event lifecycle
        /// - Used by messaging infrastructure for deduplication
        /// 
        /// Common Implementation Strategies:
        /// - GUID/UUID generation for guaranteed uniqueness
        /// - Composite keys combining timestamp and sequence
        /// - Hash-based identification for content-addressable events
        /// - Snowflake-style distributed ID generation
        /// 
        /// Operational Uses:
        /// - Event deduplication in messaging systems
        /// - Idempotency checks in event handlers
        /// - Event tracking and monitoring systems
        /// - Audit trail and compliance reporting
        /// - Event replay and recovery scenarios
        /// </remarks>
        Guid EventId { get; }

        /// <summary>
        /// UTC timestamp indicating when this domain event occurred in the business context.
        /// Provides temporal ordering capabilities and supports time-based event processing,
        /// analytics, and audit trail reconstruction.
        /// </summary>
        /// <value>UTC DateTime representing when the business event occurred</value>
        /// <remarks>
        /// Temporal Tracking Benefits:
        /// - Event ordering for sequence-dependent processing
        /// - Time-based analytics and business intelligence
        /// - Audit compliance and regulatory reporting
        /// - Performance analysis and system optimization
        /// 
        /// Timestamp Accuracy Considerations:
        /// - Use UTC to eliminate time zone ambiguity
        /// - Represents business time, not infrastructure time
        /// - Consider clock synchronization in distributed systems
        /// - Use appropriate precision for business requirements
        /// 
        /// Processing Implications:
        /// - Events may arrive out of temporal order
        /// - Implement proper ordering logic in consumers
        /// - Consider time window processing for analytics
        /// - Handle clock skew in distributed environments
        /// </remarks>
        DateTime OccurredAt { get; }

        /// <summary>
        /// Optional correlation identifier for tracking related events and operations
        /// across service boundaries in distributed systems. Enables end-to-end tracing
        /// and complete request flow visibility for monitoring and debugging.
        /// </summary>
        /// <value>Correlation identifier linking related events, or null if not applicable</value>
        /// <remarks>
        /// Correlation Tracking Benefits:
        /// - End-to-end request tracing across microservices
        /// - Related event grouping for business process tracking
        /// - Error investigation and troubleshooting support
        /// - Performance analysis for complete user journeys
        /// 
        /// Implementation Patterns:
        /// - Propagate from initial API request headers
        /// - Generate for business process instances
        /// - Use hierarchical correlation for complex workflows
        /// - Include in all structured logging output
        /// 
        /// Optional Nature Justification:
        /// - Not all events are part of traced requests
        /// - System-generated events may not have correlation context
        /// - Batch processing may use different correlation strategies
        /// - Legacy integration may not support correlation
        /// 
        /// Best Practices:
        /// - Always include when correlation context is available
        /// - Use consistent format across all services
        /// - Propagate through entire event processing chain
        /// - Consider GDPR implications for user correlation data
        /// </remarks>
        string? CorrelationId { get; }

        /// <summary>
        /// Schema version number for this event type, supporting backward compatibility
        /// and safe schema evolution across service deployments. Enables gradual migration
        /// strategies and prevents breaking changes in event-driven architectures.
        /// </summary>
        /// <value>Integer version number indicating the event schema version</value>
        /// <remarks>
        /// Schema Evolution Strategy:
        /// - Increment for breaking changes only
        /// - Maintain backward compatibility for at least one version
        /// - Document all schema changes and migration paths
        /// - Support multiple versions simultaneously during transitions
        /// 
        /// Versioning Guidelines:
        /// - Start with version 1 for all new event types
        /// - Breaking changes: removed fields, type changes, required field additions
        /// - Non-breaking changes: optional field additions, documentation updates
        /// - Consider semantic versioning for complex schema evolution
        /// 
        /// Consumer Compatibility:
        /// - Implement version-specific deserialization logic
        /// - Graceful handling of unknown versions
        /// - Forward compatibility for anticipated schema changes
        /// - Comprehensive error handling for version mismatches
        /// 
        /// Deployment Coordination:
        /// - Deploy consumers before producers for new versions
        /// - Maintain dual compatibility during transition periods
        /// - Monitor version distribution across the system
        /// - Coordinate schema deprecation and removal
        /// </remarks>
        int Version { get; }
    }
}