using System;

namespace BuildingBlocks.Domain.Entities
{
    /// <summary>
    /// Abstract base class for domain entities that require audit tracking capabilities.
    /// Provides default implementation of audit fields with automatic timestamp management
    /// and standardized behavior across all auditable entities in the domain model.
    /// </summary>
    /// <remarks>
    /// This base class implements the common audit functionality required by most
    /// business entities in enterprise applications:
    /// 
    /// Design Principles:
    /// - Automatic timestamp management for creation and updates
    /// - Immutable creation audit fields after entity construction
    /// - Thread-safe property access patterns
    /// - UTC-based timestamps for global system compatibility
    /// 
    /// Inheritance Benefits:
    /// - Consistent audit behavior across all domain entities
    /// - Reduced boilerplate code in entity implementations
    /// - Centralized audit logic for easier maintenance and testing
    /// - Standardized property names and types across the domain
    /// 
    /// Usage Guidelines:
    /// - Inherit from this class for all entities requiring audit tracking
    /// - Call UpdateAuditFields() method during entity modifications
    /// - Use dependency injection to provide current user context
    /// - Consider using domain events for audit trail generation
    /// 
    /// The class is designed to be lightweight and non-intrusive while providing
    /// comprehensive audit capabilities for enterprise applications.
    /// </remarks>
    public abstract class AuditableEntity : IAuditableEntity
    {
        /// <summary>
        /// UTC timestamp indicating when the entity was first created in the system.
        /// Set automatically during construction and remains immutable thereafter.
        /// </summary>
        /// <value>UTC DateTime representing the entity creation time</value>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Identifier of the user or system that created this entity.
        /// Set during construction and remains immutable for audit integrity.
        /// </summary>
        /// <value>User identifier, system name, or service account that created the entity</value>
        public string CreatedBy { get; private set; } = string.Empty;

        /// <summary>
        /// UTC timestamp of the most recent entity modification.
        /// Updated automatically when UpdateAuditFields() is called.
        /// </summary>
        /// <value>UTC DateTime of last modification, or null if never updated</value>
        public DateTime? UpdatedAt { get; private set; }

        /// <summary>
        /// Identifier of the user or system that last modified this entity.
        /// Updated automatically when UpdateAuditFields() is called.
        /// </summary>
        /// <value>User identifier of the last modifier, or null if never updated</value>
        public string? UpdatedBy { get; private set; }

        /// <summary>
        /// Initializes a new auditable entity with creation audit information.
        /// Sets CreatedAt to current UTC time and optionally sets the creator identifier.
        /// </summary>
        /// <param name="createdBy">
        /// Identifier of the user or system creating this entity. 
        /// If not provided, should be set later through SetCreatedBy() method.
        /// </param>
        /// <remarks>
        /// Constructor Guidelines:
        /// - Always call this constructor from derived entity constructors
        /// - Provide createdBy when available during construction
        /// - Use SetCreatedBy() method if creator context is not available during construction
        /// - Consider using factory patterns for consistent entity creation
        /// 
        /// Creation Time Behavior:
        /// - CreatedAt is set to DateTime.UtcNow automatically
        /// - Timestamp precision depends on system clock resolution
        /// - Consider using high-precision timestamps for time-sensitive operations
        /// - Ensure consistent time source across distributed system components
        /// </remarks>
        protected AuditableEntity(string? createdBy = null)
        {
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy ?? string.Empty;
        }

        /// <summary>
        /// Sets the creator identifier for entities where creator context is not available during construction.
        /// Provides flexibility for dependency injection and factory pattern scenarios.
        /// </summary>
        /// <param name="createdBy">
        /// Identifier of the user or system that created this entity.
        /// Cannot be null or empty for audit integrity.
        /// </param>
        /// <remarks>
        /// Usage Scenarios:
        /// - Dependency injection of user context after construction
        /// - Factory pattern where creator is determined after instantiation
        /// - Deserialization scenarios where audit context is applied separately
        /// - Testing scenarios where specific creator context is required
        /// 
        /// Security Considerations:
        /// - Should only be called once, ideally immediately after construction
        /// - Consider making this method internal for controlled access
        /// - Validate createdBy parameter to ensure audit trail integrity
        /// - Log calls to this method for security audit purposes
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when createdBy is null or empty
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when trying to change an already set CreatedBy value
        /// </exception>
        public void SetCreatedBy(string createdBy)
        {
            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("Created by cannot be null or empty", nameof(createdBy));

            if (!string.IsNullOrEmpty(CreatedBy))
                throw new InvalidOperationException("CreatedBy can only be set once and is already set");

            CreatedBy = createdBy;
        }

        /// <summary>
        /// Updates the modification audit fields with current timestamp and modifier information.
        /// Should be called whenever the entity is modified to maintain accurate audit trails.
        /// </summary>
        /// <param name="updatedBy">
        /// Identifier of the user or system performing the modification.
        /// Cannot be null or empty for audit integrity.
        /// </param>
        /// <remarks>
        /// Update Guidelines:
        /// - Call this method before saving entity changes to persistent storage
        /// - Use current user context or system identifier for updatedBy parameter
        /// - Consider automating calls through repository save operations
        /// - Implement validation to ensure audit fields are properly maintained
        /// 
        /// Concurrency Considerations:
        /// - UpdatedAt can be used for optimistic concurrency control
        /// - Compare timestamps to detect concurrent modifications
        /// - Consider using row version columns for more robust concurrency control
        /// - Handle concurrent update scenarios gracefully in business logic
        /// 
        /// Performance Implications:
        /// - Method calls are lightweight with minimal overhead
        /// - Consider batching updates for bulk operations
        /// - Use efficient timestamp generation for high-throughput scenarios
        /// - Monitor audit field update patterns for optimization opportunities
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// Thrown when updatedBy is null or empty
        /// </exception>
        public void UpdateAuditFields(string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(updatedBy))
                throw new ArgumentException("Updated by cannot be null or empty", nameof(updatedBy));

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        /// <summary>
        /// Determines whether this entity has been modified since creation.
        /// Useful for optimizing persistence operations and change detection logic.
        /// </summary>
        /// <returns>True if the entity has been modified, false if it remains in its original state</returns>
        /// <remarks>
        /// Modification Detection:
        /// - Based on presence of UpdatedAt value
        /// - Simple boolean check for conditional logic
        /// - Useful for optimizing database operations
        /// - Supports change tracking in domain services
        /// 
        /// Use Cases:
        /// - Conditional update operations to avoid unnecessary database writes
        /// - Change notification systems and event generation
        /// - Performance optimization in bulk processing scenarios
        /// - Audit reporting and change frequency analysis
        /// </remarks>
        public bool HasBeenModified() => UpdatedAt.HasValue;

        /// <summary>
        /// Calculates the time elapsed since the entity was created.
        /// Provides temporal context for business logic and analytics operations.
        /// </summary>
        /// <returns>TimeSpan representing the age of the entity since creation</returns>
        /// <remarks>
        /// Age Calculation Benefits:
        /// - Business rule implementation (e.g., expiration policies)
        /// - Performance analysis and optimization
        /// - Data lifecycle management and archival
        /// - User experience personalization based on data age
        /// 
        /// Calculation Accuracy:
        /// - Based on UTC timestamps for consistent results
        /// - Accuracy depends on system clock precision
        /// - Consider time zone implications for user-facing calculations
        /// - Use appropriate precision for business requirements
        /// </remarks>
        public TimeSpan GetAge() => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Calculates the time elapsed since the entity was last modified.
        /// Returns TimeSpan.Zero if the entity has never been modified.
        /// </summary>
        /// <returns>TimeSpan since last modification, or TimeSpan.Zero if never modified</returns>
        /// <remarks>
        /// Staleness Detection:
        /// - Identify outdated data for refresh operations
        /// - Cache invalidation and data synchronization
        /// - Performance monitoring and optimization
        /// - Data quality assessment and cleanup
        /// 
        /// Business Applications:
        /// - Implement data freshness requirements
        /// - Trigger automated update processes
        /// - User interface staleness indicators
        /// - Compliance reporting for data currency
        /// </remarks>
        public TimeSpan GetTimeSinceLastModification()
        {
            return UpdatedAt.HasValue 
                ? DateTime.UtcNow - UpdatedAt.Value 
                : TimeSpan.Zero;
        }
    }
}