namespace InventoryApi.Domain.Entities
{
    /// <summary>
    /// Represents a processed domain event for idempotency tracking.
    /// Ensures events are processed exactly once to maintain system consistency.
    /// </summary>
    public class ProcessedEvent
    {
        /// <summary>
        /// Unique identifier for this processed event record.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The unique identifier of the original domain event.
        /// Used to detect duplicate processing attempts.
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Type name of the domain event for categorization and routing.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Order identifier if the event relates to an order.
        /// </summary>
        public Guid? OrderId { get; set; }

        /// <summary>
        /// UTC timestamp when the event was processed.
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Correlation ID for distributed tracing.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Additional processing details or metadata.
        /// </summary>
        public string? ProcessingDetails { get; set; }
    }
}