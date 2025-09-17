namespace InventoryApi.Domain.Entities
{
    /// <summary>
    /// Represents the possible states of a stock reservation in the order fulfillment lifecycle.
    /// Defines the progression from initial reservation through final disposition.
    /// </summary>
    public enum ReservationStatus
    {
        /// <summary>
        /// Stock has been reserved for an order but not yet committed.
        /// </summary>
        Reserved = 1,

        /// <summary>
        /// Reserved stock has been permanently debited from inventory due to successful order completion.
        /// </summary>
        Debited = 2,

        /// <summary>
        /// Reserved stock has been released back to available inventory due to order cancellation or failure.
        /// </summary>
        Released = 3
    }

    /// <summary>
    /// Entity representing a temporary stock reservation that prevents race conditions during order processing.
    /// Implements the Saga pattern for distributed transaction management across Sales and Inventory services.
    /// </summary>
    public class StockReservation
    {
        /// <summary>
        /// Unique identifier for this stock reservation record.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Identifier of the order for which this stock reservation was created.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the product for which stock is being reserved.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Human-readable name of the product at the time of reservation creation.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of product units reserved for the associated order.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Current status of this stock reservation in the order fulfillment lifecycle.
        /// </summary>
        public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

        /// <summary>
        /// UTC timestamp when this stock reservation was initially created.
        /// </summary>
        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when this reservation was processed to its final state.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>
        /// Correlation identifier linking this reservation to the originating request chain.
        /// </summary>
        public string? CorrelationId { get; set; }
    }
}