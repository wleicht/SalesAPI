using System;

namespace BuildingBlocks.Domain.Entities
{
    /// <summary>
    /// Defines the contract for entities that require audit tracking capabilities.
    /// Provides standardized audit fields across all domain entities that need 
    /// creation and modification tracking for compliance and operational visibility.
    /// </summary>
    /// <remarks>
    /// Audit tracking is essential for:
    /// 
    /// Compliance Requirements:
    /// - Regulatory compliance (SOX, GDPR, HIPAA)
    /// - Financial audit trails
    /// - Data governance policies
    /// - Legal discovery requirements
    /// 
    /// Operational Benefits:
    /// - Change tracking and troubleshooting
    /// - Performance analysis and optimization
    /// - User behavior analytics
    /// - System health monitoring
    /// 
    /// Implementation Guidelines:
    /// - Always populate audit fields in domain services
    /// - Use UTC timestamps for consistency across time zones
    /// - Implement proper authorization for audit field updates
    /// - Consider immutability patterns for sensitive audit data
    /// 
    /// The interface promotes consistent audit implementation across all
    /// domain entities while allowing for flexible implementation strategies.
    /// </remarks>
    public interface IAuditableEntity
    {
        /// <summary>
        /// UTC timestamp indicating when the entity was first created in the system.
        /// Provides immutable creation tracking for audit trails and temporal analysis.
        /// </summary>
        /// <value>UTC DateTime representing the entity creation time</value>
        /// <remarks>
        /// Creation Time Guidelines:
        /// - Always set during entity construction or first persistence
        /// - Use UTC to avoid time zone confusion in distributed systems
        /// - Should be immutable after initial creation
        /// - Consider using DateTimeOffset for time zone awareness in global systems
        /// 
        /// Common Use Cases:
        /// - Audit logging and compliance reporting
        /// - Temporal queries for business analytics
        /// - Data retention policy enforcement
        /// - Performance tracking and system monitoring
        /// </remarks>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Identifier of the user or system that created this entity.
        /// Enables accountability and authorization tracking for audit compliance.
        /// </summary>
        /// <value>User identifier, system name, or service account that created the entity</value>
        /// <remarks>
        /// Creator Identification Strategies:
        /// - User ID: For user-initiated operations
        /// - Service Account: For system-generated entities
        /// - System Name: For batch or automated processes
        /// - API Key ID: For external system integrations
        /// 
        /// Security Considerations:
        /// - Avoid storing sensitive user information directly
        /// - Use consistent identifier format across all services
        /// - Consider GDPR implications for user data retention
        /// - Implement proper access controls for audit data
        /// </remarks>
        string CreatedBy { get; }

        /// <summary>
        /// UTC timestamp of the most recent entity modification.
        /// Supports optimistic concurrency control and change tracking analysis.
        /// </summary>
        /// <value>UTC DateTime of last modification, or null if never updated</value>
        /// <remarks>
        /// Update Tracking Benefits:
        /// - Optimistic concurrency control implementation
        /// - Change frequency analysis for performance optimization
        /// - Stale data detection in distributed systems
        /// - Conflict resolution in eventual consistency scenarios
        /// 
        /// Implementation Patterns:
        /// - Update automatically in repository save operations
        /// - Use database triggers for consistency across all updates
        /// - Consider event sourcing for complete change history
        /// - Implement proper null handling for create-only entities
        /// </remarks>
        DateTime? UpdatedAt { get; }

        /// <summary>
        /// Identifier of the user or system that last modified this entity.
        /// Provides modification accountability for audit trails and troubleshooting.
        /// </summary>
        /// <value>User identifier of the last modifier, or null if never updated</value>
        /// <remarks>
        /// Modifier Tracking Guidelines:
        /// - Update during every entity modification
        /// - Use same identifier format as CreatedBy field
        /// - Consider service context for automated updates
        /// - Implement proper authorization for audit field updates
        /// 
        /// Operational Uses:
        /// - Troubleshooting data quality issues
        /// - User activity monitoring and analytics
        /// - Change attribution in collaborative systems
        /// - Security incident investigation
        /// </remarks>
        string? UpdatedBy { get; }
    }
}