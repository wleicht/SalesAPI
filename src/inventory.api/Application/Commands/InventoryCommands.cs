using InventoryApi.Domain.ValueObjects;

namespace InventoryApi.Application.Commands
{
    /// <summary>
    /// Represents a command to create a new product in the inventory system.
    /// Encapsulates all necessary data for product creation with validation attributes
    /// and business logic enforcement for the product creation workflow.
    /// </summary>
    /// <remarks>
    /// Command Design Principles:
    /// 
    /// CQRS Implementation:
    /// - Represents an intent to change system state
    /// - Contains all necessary data for the operation
    /// - Immutable after creation to ensure command integrity
    /// - Clear separation between commands and queries
    /// 
    /// Validation Strategy:
    /// - Data validation at command level
    /// - Business rule validation in domain services
    /// - Cross-cutting validation through decorators
    /// - Comprehensive error handling and reporting
    /// 
    /// Use Case Alignment:
    /// - Directly maps to "Add Product" use case
    /// - Contains complete product context
    /// - Supports single responsibility principle
    /// - Enables clear audit trails and logging
    /// </remarks>
    public record CreateProductCommand
    {
        /// <summary>
        /// Name of the product for catalog display and search.
        /// Must be unique within active products and non-empty.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Detailed description of the product for customer information.
        /// Provides comprehensive product details and specifications.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Selling price of the product for order processing.
        /// Must be non-negative and represent current market price.
        /// </summary>
        public decimal Price { get; init; }

        /// <summary>
        /// Initial stock quantity when adding the product.
        /// Represents available inventory for immediate ordering.
        /// </summary>
        public int InitialStock { get; init; }

        /// <summary>
        /// Minimum stock level that triggers replenishment alerts.
        /// Used for automated inventory management and procurement planning.
        /// </summary>
        public int MinimumStockLevel { get; init; } = 10;

        /// <summary>
        /// Identifier of the user or system creating the product.
        /// Required for audit trails and authorization validation.
        /// </summary>
        public string CreatedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing across distributed systems.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to update existing product information.
    /// Supports modification of product catalog data and business attributes.
    /// </summary>
    public record UpdateProductCommand
    {
        /// <summary>
        /// Unique identifier of the product to update.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Updated name of the product (optional).
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Updated description of the product (optional).
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Updated price of the product (optional).
        /// </summary>
        public decimal? Price { get; init; }

        /// <summary>
        /// Updated minimum stock level (optional).
        /// </summary>
        public int? MinimumStockLevel { get; init; }

        /// <summary>
        /// Identifier of the user making the update.
        /// </summary>
        public string UpdatedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to reserve stock for a pending order.
    /// Initiates the stock reservation process with comprehensive validation.
    /// </summary>
    /// <remarks>
    /// Stock Reservation:
    /// - Temporarily reduces available stock
    /// - Prevents overselling in concurrent scenarios
    /// - Can be converted to allocation or released
    /// - Supports order processing workflows
    /// </remarks>
    public record ReserveStockCommand
    {
        /// <summary>
        /// Unique identifier of the product for stock reservation.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Quantity of stock to reserve for the order.
        /// </summary>
        public int Quantity { get; init; }

        /// <summary>
        /// Identifier of the order requiring stock reservation.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Identifier of the customer placing the order.
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Identifier of the user or system making the reservation.
        /// </summary>
        public string ReservedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to allocate reserved stock for a confirmed order.
    /// Converts temporary reservations to firm inventory commitments.
    /// </summary>
    /// <remarks>
    /// Stock Allocation:
    /// - Converts reservations to firm allocations
    /// - Represents final inventory commitment
    /// - Triggers fulfillment and shipping processes
    /// - Cannot be easily reversed once allocated
    /// </remarks>
    public record AllocateStockCommand
    {
        /// <summary>
        /// Unique identifier of the product for stock allocation.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Quantity of stock to allocate for the order.
        /// </summary>
        public int Quantity { get; init; }

        /// <summary>
        /// Identifier of the confirmed order.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Identifier of the customer.
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Identifier of the user or system making the allocation.
        /// </summary>
        public string AllocatedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to release reserved stock back to available inventory.
    /// Handles stock release during order cancellation scenarios.
    /// </summary>
    /// <remarks>
    /// Stock Release:
    /// - Returns reserved stock to available inventory
    /// - Occurs during order cancellation
    /// - May resolve out-of-stock conditions
    /// - Enables other customers to order released products
    /// </remarks>
    public record ReleaseStockCommand
    {
        /// <summary>
        /// Unique identifier of the product for stock release.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Quantity of stock to release back to available inventory.
        /// </summary>
        public int Quantity { get; init; }

        /// <summary>
        /// Identifier of the cancelled order.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Identifier of the customer.
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Identifier of the user or system releasing the stock.
        /// </summary>
        public string ReleasedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional reason for the stock release.
        /// </summary>
        public string? ReleaseReason { get; init; }

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to adjust product stock levels for inventory corrections.
    /// Supports both positive and negative adjustments with audit requirements.
    /// </summary>
    /// <remarks>
    /// Stock Adjustment:
    /// - Inventory count corrections and reconciliations
    /// - Damage, theft, or loss reporting
    /// - Returns processing and restocking
    /// - Vendor returns and inventory adjustments
    /// </remarks>
    public record AdjustStockCommand
    {
        /// <summary>
        /// Unique identifier of the product for stock adjustment.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Quantity to adjust (positive for addition, negative for subtraction).
        /// </summary>
        public int AdjustmentQuantity { get; init; }

        /// <summary>
        /// Reason for the stock adjustment (required for audit compliance).
        /// </summary>
        public string AdjustmentReason { get; init; } = string.Empty;

        /// <summary>
        /// Identifier of the user making the adjustment.
        /// </summary>
        public string AdjustedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to activate a product for sale and ordering.
    /// Enables product visibility in catalog and ordering systems.
    /// </summary>
    public record ActivateProductCommand
    {
        /// <summary>
        /// Unique identifier of the product to activate.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Identifier of the user activating the product.
        /// </summary>
        public string ActivatedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }

    /// <summary>
    /// Represents a command to deactivate a product, removing it from sale availability.
    /// Supports product retirement and lifecycle management.
    /// </summary>
    public record DeactivateProductCommand
    {
        /// <summary>
        /// Unique identifier of the product to deactivate.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Identifier of the user deactivating the product.
        /// </summary>
        public string DeactivatedBy { get; init; } = string.Empty;

        /// <summary>
        /// Optional reason for deactivation.
        /// </summary>
        public string? DeactivationReason { get; init; }

        /// <summary>
        /// Optional correlation identifier for request tracing.
        /// </summary>
        public string? CorrelationId { get; init; }
    }
}