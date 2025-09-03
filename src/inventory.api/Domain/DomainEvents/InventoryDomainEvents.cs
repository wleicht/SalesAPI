using BuildingBlocks.Events.Domain;
using InventoryApi.Domain.ValueObjects;

namespace InventoryApi.Domain.DomainEvents
{
    /// <summary>
    /// Domain event published when stock is reserved for a pending order.
    /// Notifies other bounded contexts about inventory allocation and supports
    /// order processing workflows, analytics, and inventory planning systems.
    /// </summary>
    /// <remarks>
    /// Stock reservation represents a critical step in the order lifecycle:
    /// 
    /// Business Process Context:
    /// - Occurs during order validation and processing
    /// - Temporarily reduces available stock for pending orders
    /// - Prevents overselling in concurrent order scenarios
    /// - Can be converted to allocation or released on cancellation
    /// 
    /// Integration Scenarios:
    /// - Order processing systems tracking reservation status
    /// - Inventory planning systems monitoring stock availability
    /// - Analytics systems tracking demand and reservation patterns
    /// - Customer service systems providing stock visibility
    /// 
    /// Event Timing:
    /// - Published after successful stock reservation
    /// - Indicates temporary commitment of inventory
    /// - Precedes order confirmation in typical workflow
    /// - May be followed by allocation or release events
    /// 
    /// The event design ensures accurate inventory tracking and supports
    /// reliable order processing in distributed e-commerce systems.
    /// </remarks>
    public class StockReservedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the product for which stock was reserved.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Name of the product for display and audit purposes.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of stock that was reserved.
        /// </summary>
        public StockQuantity ReservedQuantity { get; set; }

        /// <summary>
        /// Remaining available stock after the reservation.
        /// </summary>
        public StockQuantity RemainingStock { get; set; }

        /// <summary>
        /// Identifier of the order for which stock was reserved.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the customer who placed the order.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// UTC timestamp when the stock was reserved.
        /// </summary>
        public DateTime ReservedAt { get; set; }

        /// <summary>
        /// Identifier of the user or system that made the reservation.
        /// </summary>
        public string ReservedBy { get; set; } = string.Empty;

        /// <summary>
        /// Default constructor for serialization frameworks.
        /// </summary>
        public StockReservedDomainEvent()
        {
            ReservedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a stock reserved event with complete reservation context.
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="productName">Product name</param>
        /// <param name="reservedQuantity">Quantity reserved</param>
        /// <param name="remainingStock">Remaining available stock</param>
        /// <param name="orderId">Order identifier</param>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="reservedBy">User or system making the reservation</param>
        /// <param name="correlationId">Optional correlation ID for tracing</param>
        public StockReservedDomainEvent(
            Guid productId,
            string productName,
            StockQuantity reservedQuantity,
            StockQuantity remainingStock,
            Guid orderId,
            Guid customerId,
            string reservedBy,
            string? correlationId = null)
        {
            ProductId = productId;
            ProductName = productName;
            ReservedQuantity = reservedQuantity;
            RemainingStock = remainingStock;
            OrderId = orderId;
            CustomerId = customerId;
            ReservedAt = DateTime.UtcNow;
            ReservedBy = reservedBy;
            CorrelationId = correlationId;
        }
    }

    /// <summary>
    /// Domain event published when reserved stock is allocated for a confirmed order.
    /// Represents the transition from temporary reservation to firm allocation
    /// for order fulfillment and inventory commitment.
    /// </summary>
    /// <remarks>
    /// Stock allocation represents firm inventory commitment:
    /// 
    /// Business Significance:
    /// - Converts temporary reservations to firm allocations
    /// - Occurs when orders are confirmed for fulfillment
    /// - Represents final commitment of inventory to customer
    /// - Cannot be easily reversed once allocated
    /// 
    /// Process Integration:
    /// - Follows successful order confirmation
    /// - Triggers fulfillment and shipping processes
    /// - Updates inventory metrics and reporting
    /// - May trigger replenishment if below minimum levels
    /// 
    /// Downstream Impact:
    /// - Fulfillment systems begin order preparation
    /// - Inventory planning systems update forecasts
    /// - Analytics systems track allocation patterns
    /// - Customer service systems update order status
    /// 
    /// The event ensures accurate inventory commitment tracking
    /// and reliable coordination across fulfillment processes.
    /// </remarks>
    public class StockAllocatedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the product for which stock was allocated.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Name of the product for display and audit purposes.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of stock that was allocated.
        /// </summary>
        public StockQuantity AllocatedQuantity { get; set; }

        /// <summary>
        /// Remaining available stock after the allocation.
        /// </summary>
        public StockQuantity RemainingStock { get; set; }

        /// <summary>
        /// Identifier of the order for which stock was allocated.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the customer who placed the order.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// UTC timestamp when the stock was allocated.
        /// </summary>
        public DateTime AllocatedAt { get; set; }

        /// <summary>
        /// Identifier of the user or system that made the allocation.
        /// </summary>
        public string AllocatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Default constructor for serialization frameworks.
        /// </summary>
        public StockAllocatedDomainEvent()
        {
            AllocatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a stock allocated event with complete allocation context.
        /// </summary>
        public StockAllocatedDomainEvent(
            Guid productId,
            string productName,
            StockQuantity allocatedQuantity,
            StockQuantity remainingStock,
            Guid orderId,
            Guid customerId,
            string allocatedBy,
            string? correlationId = null)
        {
            ProductId = productId;
            ProductName = productName;
            AllocatedQuantity = allocatedQuantity;
            RemainingStock = remainingStock;
            OrderId = orderId;
            CustomerId = customerId;
            AllocatedAt = DateTime.UtcNow;
            AllocatedBy = allocatedBy;
            CorrelationId = correlationId;
        }
    }

    /// <summary>
    /// Domain event published when reserved stock is released back to available inventory.
    /// Occurs during order cancellation scenarios and supports inventory recovery workflows.
    /// </summary>
    /// <remarks>
    /// Stock release ensures accurate inventory recovery:
    /// 
    /// Release Scenarios:
    /// - Order cancellation before confirmation
    /// - Payment processing failures
    /// - Customer-initiated cancellations
    /// - System-detected issues requiring order cleanup
    /// 
    /// Business Impact:
    /// - Returns reserved stock to available inventory
    /// - Enables other customers to order released products
    /// - Maintains accurate inventory availability
    /// - Prevents permanent stock loss from failed orders
    /// 
    /// Process Coordination:
    /// - Follows order cancellation events
    /// - Updates inventory availability metrics
    /// - May resolve out-of-stock conditions
    /// - Supports customer service scenarios
    /// 
    /// The event ensures proper inventory cleanup and recovery
    /// during order cancellation and failure scenarios.
    /// </remarks>
    public class StockReleasedDomainEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the product for which stock was released.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Name of the product for display and audit purposes.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of stock that was released back to available inventory.
        /// </summary>
        public StockQuantity ReleasedQuantity { get; set; }

        /// <summary>
        /// Total available stock after the release.
        /// </summary>
        public StockQuantity NewAvailableStock { get; set; }

        /// <summary>
        /// Identifier of the order for which stock was released.
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Identifier of the customer whose order was cancelled.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// UTC timestamp when the stock was released.
        /// </summary>
        public DateTime ReleasedAt { get; set; }

        /// <summary>
        /// Identifier of the user or system that released the stock.
        /// </summary>
        public string ReleasedBy { get; set; } = string.Empty;

        /// <summary>
        /// Optional reason for the stock release for audit purposes.
        /// </summary>
        public string? ReleaseReason { get; set; }

        /// <summary>
        /// Default constructor for serialization frameworks.
        /// </summary>
        public StockReleasedDomainEvent()
        {
            ReleasedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a stock released event with complete release context.
        /// </summary>
        public StockReleasedDomainEvent(
            Guid productId,
            string productName,
            StockQuantity releasedQuantity,
            StockQuantity newAvailableStock,
            Guid orderId,
            Guid customerId,
            string releasedBy,
            string? releaseReason = null,
            string? correlationId = null)
        {
            ProductId = productId;
            ProductName = productName;
            ReleasedQuantity = releasedQuantity;
            NewAvailableStock = newAvailableStock;
            OrderId = orderId;
            CustomerId = customerId;
            ReleasedAt = DateTime.UtcNow;
            ReleasedBy = releasedBy;
            ReleaseReason = releaseReason;
            CorrelationId = correlationId;
        }
    }

    /// <summary>
    /// Domain event published when stock levels fall below minimum thresholds.
    /// Triggers replenishment workflows and inventory planning processes.
    /// </summary>
    /// <remarks>
    /// Low stock alerts support proactive inventory management:
    /// 
    /// Alert Conditions:
    /// - Current stock at or below minimum threshold
    /// - Triggered by sales, allocations, or adjustments
    /// - Prevents stockouts and customer disappointment
    /// - Enables proactive procurement planning
    /// 
    /// Business Processes:
    /// - Procurement teams initiate purchase orders
    /// - Inventory planners adjust forecasts
    /// - Category managers review demand patterns
    /// - Customer service prepares for potential stockouts
    /// 
    /// Automation Opportunities:
    /// - Automatic purchase order generation
    /// - Supplier notification systems
    /// - Customer backorder management
    /// - Alternative product recommendations
    /// 
    /// The event enables proactive inventory management and
    /// prevents stockout situations through early warning systems.
    /// </remarks>
    public class LowStockAlertDomainEvent : DomainEvent
    {
        /// <summary>
        /// Unique identifier of the product with low stock.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Name of the product for display and notification purposes.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Current available stock quantity.
        /// </summary>
        public StockQuantity CurrentStock { get; set; }

        /// <summary>
        /// Minimum stock level threshold that triggered the alert.
        /// </summary>
        public StockQuantity MinimumStockLevel { get; set; }

        /// <summary>
        /// Severity level of the stock shortage.
        /// </summary>
        public StockAlertSeverity Severity { get; set; }

        /// <summary>
        /// UTC timestamp when the low stock condition was detected.
        /// </summary>
        public DateTime DetectedAt { get; set; }

        /// <summary>
        /// Category or classification of the product for prioritization.
        /// </summary>
        public string? ProductCategory { get; set; }

        /// <summary>
        /// Default constructor for serialization frameworks.
        /// </summary>
        public LowStockAlertDomainEvent()
        {
            DetectedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a low stock alert event with complete context.
        /// </summary>
        public LowStockAlertDomainEvent(
            Guid productId,
            string productName,
            StockQuantity currentStock,
            StockQuantity minimumStockLevel,
            string? productCategory = null,
            string? correlationId = null)
        {
            ProductId = productId;
            ProductName = productName;
            CurrentStock = currentStock;
            MinimumStockLevel = minimumStockLevel;
            DetectedAt = DateTime.UtcNow;
            ProductCategory = productCategory;
            CorrelationId = correlationId;

            // Calculate severity based on stock level
            Severity = CalculateSeverity(currentStock, minimumStockLevel);
        }

        /// <summary>
        /// Calculates the severity of the stock shortage.
        /// </summary>
        private static StockAlertSeverity CalculateSeverity(StockQuantity currentStock, StockQuantity minimumStock)
        {
            if (currentStock.IsZero)
                return StockAlertSeverity.Critical;
            
            if (currentStock.Value <= minimumStock.Value * 0.5)
                return StockAlertSeverity.High;
            
            if (currentStock.Value <= minimumStock.Value * 0.8)
                return StockAlertSeverity.Medium;
            
            return StockAlertSeverity.Low;
        }
    }

    /// <summary>
    /// Enumeration defining the severity levels for stock alerts.
    /// Supports prioritization and automated response systems.
    /// </summary>
    public enum StockAlertSeverity
    {
        /// <summary>
        /// Low severity - stock approaching minimum levels.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium severity - stock below comfortable levels.
        /// </summary>
        Medium = 2,

        /// <summary>
        /// High severity - stock critically low.
        /// </summary>
        High = 3,

        /// <summary>
        /// Critical severity - product out of stock.
        /// </summary>
        Critical = 4
    }
}