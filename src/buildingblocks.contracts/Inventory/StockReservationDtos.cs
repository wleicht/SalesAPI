using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BuildingBlocks.Contracts.Inventory
{
    /// <summary>
    /// Data transfer object for requesting stock reservation operations.
    /// Encapsulates the information required to reserve inventory for order processing
    /// and supports validation of reservation business rules.
    /// </summary>
    /// <remarks>
    /// This DTO is used in the synchronous stock reservation flow where the Sales API
    /// requests immediate allocation of inventory before proceeding with order confirmation.
    /// It supports the Saga pattern implementation for distributed transaction management.
    /// 
    /// Usage Context:
    /// - Pre-order validation to ensure stock availability
    /// - Atomic stock allocation to prevent race conditions
    /// - Integration between Sales and Inventory services
    /// - Foundation for order cancellation and compensation workflows
    /// </remarks>
    public class StockReservationRequest
    {
        /// <summary>
        /// Unique identifier of the order requesting stock reservation.
        /// Links the reservation request to the specific sales transaction.
        /// </summary>
        /// <value>Order GUID from the Sales service</value>
        /// <remarks>
        /// This identifier enables:
        /// - Correlation between sales orders and inventory reservations
        /// - Audit trail for order fulfillment processes
        /// - Support for order-specific reservation queries and operations
        /// - Integration with customer service and order management workflows
        /// </remarks>
        [Required(ErrorMessage = "Order ID is required for stock reservation")]
        public required Guid OrderId { get; set; }

        /// <summary>
        /// Collection of product items requiring stock reservation.
        /// Each item specifies the product and quantity to be reserved from inventory.
        /// </summary>
        /// <value>List of reservation item specifications</value>
        /// <remarks>
        /// Business Rules:
        /// - Must contain at least one item (orders cannot be empty)
        /// - Each item must specify valid product ID and positive quantity
        /// - Duplicate product IDs within same request are not allowed
        /// - Total reservation cannot exceed available inventory per product
        /// 
        /// Processing Behavior:
        /// - All items must be successfully reserved or entire request fails
        /// - Atomic operation ensures consistency across all requested items
        /// - Partial reservations are not supported to maintain order integrity
        /// </remarks>
        [Required(ErrorMessage = "At least one item must be specified for reservation")]
        [MinLength(1, ErrorMessage = "Reservation request must contain at least one item")]
        public required List<StockReservationItem> Items { get; set; } = new();

        /// <summary>
        /// Optional correlation identifier for distributed tracing and request tracking.
        /// Enables end-to-end visibility across service boundaries.
        /// </summary>
        /// <value>Correlation ID string for request tracing</value>
        /// <remarks>
        /// When provided, this correlation ID will be:
        /// - Propagated to all related inventory operations
        /// - Included in audit logs and monitoring data
        /// - Used for distributed tracing and performance analysis
        /// - Available for customer service and troubleshooting workflows
        /// </remarks>
        [MaxLength(100, ErrorMessage = "Correlation ID cannot exceed 100 characters")]
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// Represents a single product item within a stock reservation request.
    /// Specifies the product identifier and quantity requiring reservation allocation.
    /// </summary>
    /// <remarks>
    /// This DTO encapsulates the atomic unit of stock reservation - a specific quantity
    /// of a particular product. It includes validation rules to ensure business
    /// consistency and prevent invalid reservation requests.
    /// </remarks>
    public class StockReservationItem
    {
        /// <summary>
        /// Unique identifier of the product requiring stock reservation.
        /// Must reference an existing product in the inventory system.
        /// </summary>
        /// <value>Product GUID that identifies the inventory item</value>
        /// <remarks>
        /// Validation Requirements:
        /// - Must be a valid GUID format
        /// - Must reference an existing product in the inventory database
        /// - Product must have sufficient available stock for the requested quantity
        /// - Product must be active and available for reservation
        /// </remarks>
        [Required(ErrorMessage = "Product ID is required for stock reservation")]
        public required Guid ProductId { get; set; }

        /// <summary>
        /// Quantity of the product to reserve from available inventory.
        /// Must be a positive integer not exceeding available stock levels.
        /// </summary>
        /// <value>Number of units to reserve</value>
        /// <remarks>
        /// Business Rules:
        /// - Must be at least 1 (cannot reserve zero or negative quantities)
        /// - Cannot exceed current available inventory for the product
        /// - Validated against real-time stock levels at reservation time
        /// - Supports integer quantities only (no fractional units)
        /// 
        /// Inventory Impact:
        /// - Reserved quantity becomes unavailable for other reservations
        /// - Available-to-promise inventory is reduced by this amount
        /// - Physical inventory remains unchanged until order confirmation
        /// </remarks>
        [Required(ErrorMessage = "Quantity is required for stock reservation")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public required int Quantity { get; set; }
    }

    /// <summary>
    /// Response object containing the results of a stock reservation operation.
    /// Provides comprehensive information about reservation success, failure details,
    /// and resulting reservation identifiers for downstream processing.
    /// </summary>
    /// <remarks>
    /// This response supports both successful and failed reservation scenarios,
    /// enabling the calling service to make appropriate decisions about order
    /// processing continuation or failure handling.
    /// 
    /// Success Scenarios:
    /// - All requested items successfully reserved
    /// - Reservation IDs provided for future reference and event correlation
    /// - Ready to proceed with order confirmation workflow
    /// 
    /// Failure Scenarios:
    /// - Insufficient stock for one or more requested items
    /// - Product not found or inactive
    /// - System errors during reservation processing
    /// - Business rule violations preventing reservation
    /// </remarks>
    public class StockReservationResponse
    {
        /// <summary>
        /// Indicates whether the entire stock reservation request was successful.
        /// True when all requested items were successfully reserved; false otherwise.
        /// </summary>
        /// <value>Boolean indicating overall reservation success</value>
        /// <remarks>
        /// Success Criteria:
        /// - All requested products exist and are active
        /// - Sufficient inventory available for all requested quantities
        /// - No system errors during reservation processing
        /// - All business validation rules satisfied
        /// 
        /// When false, examine ErrorMessage and ReservationResults for specific
        /// failure details and appropriate error handling strategies.
        /// </remarks>
        public bool Success { get; set; }

        /// <summary>
        /// Detailed error message when reservation fails, or null for successful operations.
        /// Provides actionable information for error handling and user communication.
        /// </summary>
        /// <value>Human-readable error description or null if successful</value>
        /// <remarks>
        /// Error Message Categories:
        /// - Insufficient Stock: "Insufficient stock for product [ProductName]. Available: [X], Requested: [Y]"
        /// - Product Not Found: "Product [ProductId] not found or is inactive"
        /// - System Errors: "Unable to process reservation due to system error"
        /// - Business Rules: "Reservation violates business rule: [RuleDescription]"
        /// 
        /// Usage Guidelines:
        /// - Customer Communication: User-friendly messages suitable for customer display
        /// - Technical Debugging: Sufficient detail for development troubleshooting
        /// - Logging Integration: Structured format compatible with monitoring systems
        /// - Localization Ready: Messages designed for future internationalization
        /// </remarks>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Collection of individual reservation results for each requested item.
        /// Provides detailed success/failure information at the product level.
        /// </summary>
        /// <value>List of per-item reservation outcomes</value>
        /// <remarks>
        /// Detailed Results Benefits:
        /// - Granular Success/Failure: Identify which specific products caused failures
        /// - Partial Analysis: Understand impact of partial stock availability
        /// - Business Intelligence: Data for inventory optimization and demand planning
        /// - Customer Service: Detailed information for customer inquiry resolution
        /// 
        /// All-or-Nothing Behavior:
        /// While detailed results are provided, the reservation operation follows
        /// all-or-nothing semantics - either all items are reserved successfully
        /// or no reservations are created (atomic operation).
        /// </remarks>
        public List<StockReservationResult> ReservationResults { get; set; } = new();

        /// <summary>
        /// Total number of distinct products processed in this reservation request.
        /// Provides quick assessment of request complexity and scope.
        /// </summary>
        /// <value>Count of unique products in the reservation request</value>
        public int TotalItemsProcessed => ReservationResults.Count;
    }

    /// <summary>
    /// Detailed result information for a single product within a stock reservation operation.
    /// Provides complete success/failure context for individual inventory items.
    /// </summary>
    /// <remarks>
    /// This granular result enables detailed analysis of reservation outcomes
    /// and supports sophisticated error handling and business intelligence scenarios.
    /// </remarks>
    public class StockReservationResult
    {
        /// <summary>
        /// Identifier of the product that was processed in this reservation operation.
        /// </summary>
        /// <value>Product GUID from the original reservation request</value>
        public required Guid ProductId { get; set; }

        /// <summary>
        /// Human-readable name of the product for display and logging purposes.
        /// </summary>
        /// <value>Product name at the time of reservation processing</value>
        public required string ProductName { get; set; }

        /// <summary>
        /// Quantity that was requested for reservation for this product.
        /// </summary>
        /// <value>Original requested quantity from the reservation request</value>
        public required int RequestedQuantity { get; set; }

        /// <summary>
        /// Indicates whether the reservation was successful for this specific product.
        /// </summary>
        /// <value>True if product reservation succeeded; false otherwise</value>
        public bool Success { get; set; }

        /// <summary>
        /// Unique identifier of the created stock reservation record.
        /// Only populated for successful reservations; null for failures.
        /// </summary>
        /// <value>Reservation ID for successful operations or null for failures</value>
        /// <remarks>
        /// The ReservationId serves as:
        /// - Primary key for future reservation operations (cancellation, confirmation)
        /// - Event correlation identifier for asynchronous processing
        /// - Audit trail reference for inventory movement tracking
        /// - Customer service reference for order inquiries
        /// </remarks>
        public Guid? ReservationId { get; set; }

        /// <summary>
        /// Stock level that was available before this reservation attempt.
        /// Provides context for understanding reservation success or failure.
        /// </summary>
        /// <value>Available stock quantity before reservation processing</value>
        public int AvailableStock { get; set; }

        /// <summary>
        /// Error message specific to this product if reservation failed.
        /// Null for successful reservations.
        /// </summary>
        /// <value>Product-specific error description or null if successful</value>
        /// <remarks>
        /// Common Error Scenarios:
        /// - "Insufficient stock: available [X], requested [Y]"
        /// - "Product is inactive or discontinued"
        /// - "Product not found in inventory"
        /// - "Reservation limit exceeded for this product"
        /// </remarks>
        public string? ErrorMessage { get; set; }
    }
}