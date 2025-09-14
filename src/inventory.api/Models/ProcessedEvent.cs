using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Models
{
    /// <summary>
    /// Entity that maintains an audit trail of processed domain events to ensure idempotent message processing.
    /// Prevents duplicate execution of business operations when the same event is delivered multiple times.
    /// </summary>
    public class ProcessedEvent
    {
        /// <summary>
        /// Database primary key for this processed event record.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The original unique identifier from the domain event that was processed.
        /// Used for idempotency checking to prevent duplicate processing.
        /// </summary>
        [Required]
        public required Guid EventId { get; set; }

        /// <summary>
        /// The type name of the domain event that was processed (e.g., "OrderConfirmedEvent").
        /// </summary>
        [Required]
        [MaxLength(200)]
        public required string EventType { get; set; }

        /// <summary>
        /// The business order identifier associated with this event processing operation.
        /// Null for non-order-related events.
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// UTC timestamp when this event was successfully processed and committed.
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Correlation identifier for distributed tracing across service boundaries.
        /// </summary>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Optional supplementary information about the event processing outcome or errors.
        /// </summary>
        [MaxLength(1000)]
        public string? ProcessingDetails { get; set; }
    }
}