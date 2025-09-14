using System;

namespace BuildingBlocks.Domain.Entities
{
    /// <summary>
    /// Abstract base class for domain entities that require audit tracking capabilities.
    /// Provides automatic timestamp management and standardized audit behavior.
    /// </summary>
    public abstract class AuditableEntity : IAuditableEntity
    {
        /// <summary>
        /// UTC timestamp when the entity was created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Identifier of the user or system that created this entity.
        /// </summary>
        public string CreatedBy { get; private set; } = string.Empty;

        /// <summary>
        /// UTC timestamp of the most recent entity modification.
        /// </summary>
        public DateTime? UpdatedAt { get; private set; }

        /// <summary>
        /// Identifier of the user or system that last modified this entity.
        /// </summary>
        public string? UpdatedBy { get; private set; }

        /// <summary>
        /// Initializes a new auditable entity with creation audit information.
        /// </summary>
        /// <param name="createdBy">Identifier of the user or system creating this entity</param>
        protected AuditableEntity(string? createdBy = null)
        {
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy ?? string.Empty;
        }

        /// <summary>
        /// Sets the creator identifier for entities where creator context is not available during construction.
        /// Can only be called once per entity instance.
        /// </summary>
        /// <param name="createdBy">Creator identifier</param>
        /// <exception cref="ArgumentException">Thrown when createdBy is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when CreatedBy is already set</exception>
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
        /// </summary>
        /// <param name="updatedBy">Identifier of the user or system performing the modification</param>
        /// <exception cref="ArgumentException">Thrown when updatedBy is null or empty</exception>
        public void UpdateAuditFields(string updatedBy)
        {
            if (string.IsNullOrWhiteSpace(updatedBy))
                throw new ArgumentException("Updated by cannot be null or empty", nameof(updatedBy));

            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        /// <summary>
        /// Determines whether this entity has been modified since creation.
        /// </summary>
        /// <returns>True if the entity has been modified</returns>
        public bool HasBeenModified() => UpdatedAt.HasValue;

        /// <summary>
        /// Calculates the time elapsed since the entity was created.
        /// </summary>
        /// <returns>TimeSpan representing the age of the entity</returns>
        public TimeSpan GetAge() => DateTime.UtcNow - CreatedAt;

        /// <summary>
        /// Calculates the time elapsed since the entity was last modified.
        /// Returns TimeSpan.Zero if the entity has never been modified.
        /// </summary>
        /// <returns>TimeSpan since last modification</returns>
        public TimeSpan GetTimeSinceLastModification()
        {
            return UpdatedAt.HasValue 
                ? DateTime.UtcNow - UpdatedAt.Value 
                : TimeSpan.Zero;
        }
    }
}