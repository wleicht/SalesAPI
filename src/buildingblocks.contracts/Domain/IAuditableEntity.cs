using System;

namespace BuildingBlocks.Domain.Entities
{
    /// <summary>
    /// Defines the contract for entities that require audit tracking capabilities.
    /// Provides standardized audit fields for creation and modification tracking.
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// UTC timestamp when the entity was created.
        /// </summary>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Identifier of the user or system that created this entity.
        /// </summary>
        string CreatedBy { get; }

        /// <summary>
        /// UTC timestamp of the most recent entity modification.
        /// </summary>
        DateTime? UpdatedAt { get; }

        /// <summary>
        /// Identifier of the user or system that last modified this entity.
        /// </summary>
        string? UpdatedBy { get; }
    }
}