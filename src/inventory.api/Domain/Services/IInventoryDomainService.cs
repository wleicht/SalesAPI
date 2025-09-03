using InventoryApi.Domain.Entities;
using InventoryApi.Domain.ValueObjects;
using InventoryApi.Domain.DomainEvents;
using InventoryApi.Domain.Repositories;
using BuildingBlocks.Infrastructure.Messaging;

namespace InventoryApi.Domain.Services
{
    /// <summary>
    /// Defines the contract for inventory domain services responsible for complex business operations
    /// that span multiple entities or require cross-cutting concerns. Orchestrates inventory management
    /// workflows including stock operations, event publishing, and business rule enforcement.
    /// </summary>
    /// <remarks>
    /// Domain Service Responsibilities:
    /// 
    /// Business Process Orchestration:
    /// - Complex inventory operations involving multiple products
    /// - Cross-aggregate business logic and validation
    /// - Domain event coordination and publishing
    /// - Business rule enforcement across inventory lifecycle
    /// 
    /// Integration Coordination:
    /// - External service integration patterns
    /// - Cross-domain communication and coordination
    /// - Event-driven architecture participation
    /// - Distributed transaction coordination
    /// 
    /// Business Logic Centralization:
    /// - Inventory state management and transitions
    /// - Complex validation and business rule enforcement
    /// - Performance optimization for inventory operations
    /// - Consistency maintenance across stock operations
    /// 
    /// The interface follows Domain-Driven Design principles by encapsulating
    /// business logic that doesn't naturally fit within entity boundaries.
    /// </remarks>
    public interface IInventoryDomainService
    {
        /// <summary>
        /// Reserves stock for a pending order with comprehensive validation and event publishing.
        /// Orchestrates the complete stock reservation process including availability checks,
        /// stock allocation, and appropriate event publishing for downstream processing.
        /// </summary>
        /// <param name="productId">Identifier of the product for stock reservation</param>
        /// <param name="quantity">Quantity of stock to reserve</param>
        /// <param name="orderId">Identifier of the order requiring stock</param>
        /// <param name="customerId">Identifier of the customer placing the order</param>
        /// <param name="reservedBy">Identifier of the user or system making the reservation</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Reservation result with success status and remaining stock information</returns>
        /// <remarks>
        /// Reservation Process:
        /// 1. Validate product exists and is active
        /// 2. Check sufficient stock availability
        /// 3. Reserve stock using product domain methods
        /// 4. Persist changes with transaction management
        /// 5. Publish StockReservedDomainEvent for downstream processing
        /// 
        /// Business Rules:
        /// - Product must be active for reservations
        /// - Sufficient stock must be available
        /// - Quantity must be positive
        /// - Concurrent reservations handled safely
        /// 
        /// Event Integration:
        /// - StockReservedDomainEvent published after successful reservation
        /// - Correlation ID propagated for distributed tracing
        /// - Event contains complete reservation context
        /// - Enables order processing and analytics workflows
        /// </remarks>
        Task<StockReservationResult> ReserveStockAsync(
            Guid productId,
            int quantity,
            Guid orderId,
            Guid customerId,
            string reservedBy,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Allocates previously reserved stock for a confirmed order.
        /// Orchestrates the transition from reservation to firm allocation with
        /// appropriate event publishing and business rule enforcement.
        /// </summary>
        /// <param name="productId">Identifier of the product for stock allocation</param>
        /// <param name="quantity">Quantity of stock to allocate</param>
        /// <param name="orderId">Identifier of the confirmed order</param>
        /// <param name="customerId">Identifier of the customer</param>
        /// <param name="allocatedBy">Identifier of the user or system making the allocation</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Allocation result with success status and stock information</returns>
        /// <remarks>
        /// Allocation Process:
        /// 1. Validate product and order context
        /// 2. Allocate stock using product domain methods
        /// 3. Check for low stock conditions
        /// 4. Persist changes with audit trail
        /// 5. Publish StockAllocatedDomainEvent and potential LowStockAlert
        /// 
        /// Business Rules:
        /// - Stock must be previously reserved or available
        /// - Order must be in confirmed status
        /// - Allocation represents firm inventory commitment
        /// - Low stock alerts triggered if below minimum
        /// 
        /// Event Integration:
        /// - StockAllocatedDomainEvent for fulfillment systems
        /// - LowStockAlertDomainEvent if minimum threshold reached
        /// - Complete allocation context for autonomous processing
        /// - Analytics and reporting system integration
        /// </remarks>
        Task<StockAllocationResult> AllocateStockAsync(
            Guid productId,
            int quantity,
            Guid orderId,
            Guid customerId,
            string allocatedBy,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases previously reserved stock back to available inventory.
        /// Orchestrates stock release during order cancellation scenarios with
        /// proper cleanup and event publishing for system coordination.
        /// </summary>
        /// <param name="productId">Identifier of the product for stock release</param>
        /// <param name="quantity">Quantity of stock to release</param>
        /// <param name="orderId">Identifier of the cancelled order</param>
        /// <param name="customerId">Identifier of the customer</param>
        /// <param name="releasedBy">Identifier of the user or system releasing stock</param>
        /// <param name="releaseReason">Optional reason for stock release</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Release result with success status and updated stock information</returns>
        /// <remarks>
        /// Release Process:
        /// 1. Validate product and release context
        /// 2. Release stock using product domain methods
        /// 3. Update inventory availability
        /// 4. Persist changes with audit information
        /// 5. Publish StockReleasedDomainEvent for system coordination
        /// 
        /// Business Impact:
        /// - Returns stock to available inventory
        /// - May resolve out-of-stock conditions
        /// - Enables other customers to order released products
        /// - Updates inventory metrics and reporting
        /// 
        /// Event Integration:
        /// - StockReleasedDomainEvent for inventory systems
        /// - Complete release context for audit and analytics
        /// - Enables customer notification and availability updates
        /// - Supports inventory planning and forecasting
        /// </remarks>
        Task<StockReleaseResult> ReleaseStockAsync(
            Guid productId,
            int quantity,
            Guid orderId,
            Guid customerId,
            string releasedBy,
            string? releaseReason = null,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adjusts product stock levels for inventory corrections and management.
        /// Supports both positive and negative adjustments with comprehensive audit trails
        /// and business rule enforcement for inventory accuracy maintenance.
        /// </summary>
        /// <param name="productId">Identifier of the product for stock adjustment</param>
        /// <param name="adjustmentQuantity">Quantity to adjust (positive for addition, negative for subtraction)</param>
        /// <param name="adjustmentReason">Reason for the stock adjustment</param>
        /// <param name="adjustedBy">Identifier of the user making the adjustment</param>
        /// <param name="correlationId">Optional correlation ID for request tracing</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Adjustment result with success status and updated stock information</returns>
        /// <remarks>
        /// Adjustment Scenarios:
        /// - Inventory count corrections and reconciliations
        /// - Damage, theft, or loss reporting
        /// - Returns processing and restocking
        /// - Vendor returns and inventory adjustments
        /// 
        /// Validation Rules:
        /// - Product must exist and be manageable
        /// - Negative adjustments cannot exceed available stock
        /// - Adjustment reason required for audit compliance
        /// - Authorization levels for large adjustments
        /// 
        /// Audit Requirements:
        /// - Complete adjustment history and traceability
        /// - User authorization and responsibility tracking
        /// - Adjustment reason documentation
        /// - Impact analysis and reporting
        /// </remarks>
        Task<StockAdjustmentResult> AdjustStockAsync(
            Guid productId,
            int adjustmentQuantity,
            string adjustmentReason,
            string adjustedBy,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if sufficient stock is available for the requested quantities across multiple products.
        /// Provides batch validation for order processing and cart scenarios with detailed feedback.
        /// </summary>
        /// <param name="stockRequirements">Collection of product stock requirements to validate</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Validation result with availability status for each product</returns>
        /// <remarks>
        /// Validation Process:
        /// - Check availability for all requested products
        /// - Provide detailed feedback for each product
        /// - Include alternative availability suggestions
        /// - Support partial availability scenarios
        /// 
        /// Business Applications:
        /// - Shopping cart validation before checkout
        /// - Order feasibility analysis
        /// - Product availability reporting
        /// - Customer service inquiries
        /// 
        /// Performance Optimization:
        /// - Batch queries for multiple products
        /// - Efficient database access patterns
        /// - Minimal network overhead
        /// - Suitable for real-time scenarios
        /// </remarks>
        Task<BatchStockValidationResult> ValidateStockAvailabilityAsync(
            IEnumerable<StockRequirement> stockRequirements,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Identifies products requiring replenishment based on current stock levels and business rules.
        /// Supports proactive inventory management and automated procurement workflows.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of products requiring replenishment with priority information</returns>
        /// <remarks>
        /// Replenishment Criteria:
        /// - Products below minimum stock levels
        /// - Historical demand analysis
        /// - Seasonal and trend considerations
        /// - Supplier lead times and constraints
        /// 
        /// Priority Calculation:
        /// - Severity of stock shortage
        /// - Product importance and sales velocity
        /// - Customer impact and satisfaction risk
        /// - Financial impact of stockouts
        /// 
        /// Automation Support:
        /// - Integration with procurement systems
        /// - Automatic purchase order generation
        /// - Supplier notification workflows
        /// - Inventory planning optimization
        /// </remarks>
        Task<IEnumerable<ReplenishmentRecommendation>> GetReplenishmentRecommendationsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a stock requirement for validation purposes.
    /// Used in batch stock validation scenarios for order processing.
    /// </summary>
    public class StockRequirement
    {
        /// <summary>
        /// Identifier of the product requiring stock.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity of stock required for the operation.
        /// </summary>
        public int RequiredQuantity { get; set; }

        /// <summary>
        /// Optional context information for the requirement.
        /// </summary>
        public string? Context { get; set; }
    }

    /// <summary>
    /// Represents the result of a stock reservation operation.
    /// Provides detailed information about the reservation outcome and remaining stock.
    /// </summary>
    public class StockReservationResult
    {
        /// <summary>
        /// Indicates whether the stock reservation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Product for which stock was reserved.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity that was successfully reserved.
        /// </summary>
        public int ReservedQuantity { get; set; }

        /// <summary>
        /// Remaining available stock after the reservation.
        /// </summary>
        public StockQuantity RemainingStock { get; set; }

        /// <summary>
        /// Error message if the reservation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful reservation result.
        /// </summary>
        public static StockReservationResult Success(Guid productId, int reservedQuantity, StockQuantity remainingStock) =>
            new() 
            { 
                IsSuccess = true, 
                ProductId = productId, 
                ReservedQuantity = reservedQuantity, 
                RemainingStock = remainingStock 
            };

        /// <summary>
        /// Creates a failed reservation result.
        /// </summary>
        public static StockReservationResult Failure(Guid productId, string errorMessage) =>
            new() 
            { 
                IsSuccess = false, 
                ProductId = productId, 
                ErrorMessage = errorMessage 
            };
    }

    /// <summary>
    /// Represents the result of a stock allocation operation.
    /// Provides detailed information about the allocation outcome.
    /// </summary>
    public class StockAllocationResult
    {
        /// <summary>
        /// Indicates whether the stock allocation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Product for which stock was allocated.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity that was successfully allocated.
        /// </summary>
        public int AllocatedQuantity { get; set; }

        /// <summary>
        /// Remaining available stock after the allocation.
        /// </summary>
        public StockQuantity RemainingStock { get; set; }

        /// <summary>
        /// Indicates if the allocation triggered a low stock condition.
        /// </summary>
        public bool LowStockTriggered { get; set; }

        /// <summary>
        /// Error message if the allocation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful allocation result.
        /// </summary>
        public static StockAllocationResult Success(
            Guid productId, 
            int allocatedQuantity, 
            StockQuantity remainingStock, 
            bool lowStockTriggered = false) =>
            new() 
            { 
                IsSuccess = true, 
                ProductId = productId, 
                AllocatedQuantity = allocatedQuantity, 
                RemainingStock = remainingStock,
                LowStockTriggered = lowStockTriggered
            };

        /// <summary>
        /// Creates a failed allocation result.
        /// </summary>
        public static StockAllocationResult Failure(Guid productId, string errorMessage) =>
            new() 
            { 
                IsSuccess = false, 
                ProductId = productId, 
                ErrorMessage = errorMessage 
            };
    }

    /// <summary>
    /// Represents the result of a stock release operation.
    /// Provides detailed information about the release outcome.
    /// </summary>
    public class StockReleaseResult
    {
        /// <summary>
        /// Indicates whether the stock release was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Product for which stock was released.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity that was successfully released.
        /// </summary>
        public int ReleasedQuantity { get; set; }

        /// <summary>
        /// Total available stock after the release.
        /// </summary>
        public StockQuantity NewAvailableStock { get; set; }

        /// <summary>
        /// Error message if the release failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful release result.
        /// </summary>
        public static StockReleaseResult Success(Guid productId, int releasedQuantity, StockQuantity newAvailableStock) =>
            new() 
            { 
                IsSuccess = true, 
                ProductId = productId, 
                ReleasedQuantity = releasedQuantity, 
                NewAvailableStock = newAvailableStock 
            };

        /// <summary>
        /// Creates a failed release result.
        /// </summary>
        public static StockReleaseResult Failure(Guid productId, string errorMessage) =>
            new() 
            { 
                IsSuccess = false, 
                ProductId = productId, 
                ErrorMessage = errorMessage 
            };
    }

    /// <summary>
    /// Represents the result of a stock adjustment operation.
    /// Provides detailed information about the adjustment outcome.
    /// </summary>
    public class StockAdjustmentResult
    {
        /// <summary>
        /// Indicates whether the stock adjustment was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Product for which stock was adjusted.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity that was adjusted (positive or negative).
        /// </summary>
        public int AdjustmentQuantity { get; set; }

        /// <summary>
        /// Stock quantity before the adjustment.
        /// </summary>
        public StockQuantity PreviousStock { get; set; }

        /// <summary>
        /// Stock quantity after the adjustment.
        /// </summary>
        public StockQuantity NewStock { get; set; }

        /// <summary>
        /// Error message if the adjustment failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful adjustment result.
        /// </summary>
        public static StockAdjustmentResult Success(
            Guid productId, 
            int adjustmentQuantity, 
            StockQuantity previousStock, 
            StockQuantity newStock) =>
            new() 
            { 
                IsSuccess = true, 
                ProductId = productId, 
                AdjustmentQuantity = adjustmentQuantity, 
                PreviousStock = previousStock,
                NewStock = newStock
            };

        /// <summary>
        /// Creates a failed adjustment result.
        /// </summary>
        public static StockAdjustmentResult Failure(Guid productId, string errorMessage) =>
            new() 
            { 
                IsSuccess = false, 
                ProductId = productId, 
                ErrorMessage = errorMessage 
            };
    }

    /// <summary>
    /// Represents the result of batch stock validation.
    /// Provides detailed information about availability for multiple products.
    /// </summary>
    public class BatchStockValidationResult
    {
        /// <summary>
        /// Indicates whether all requested stock is available.
        /// </summary>
        public bool AllAvailable { get; set; }

        /// <summary>
        /// Individual validation results for each product.
        /// </summary>
        public List<ProductStockValidation> ProductValidations { get; set; } = new();

        /// <summary>
        /// Summary message about the overall validation result.
        /// </summary>
        public string? Summary { get; set; }
    }

    /// <summary>
    /// Represents stock validation result for a single product.
    /// </summary>
    public class ProductStockValidation
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Requested quantity for validation.
        /// </summary>
        public int RequestedQuantity { get; set; }

        /// <summary>
        /// Available quantity for allocation.
        /// </summary>
        public int AvailableQuantity { get; set; }

        /// <summary>
        /// Indicates whether sufficient stock is available.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Maximum quantity that can be allocated.
        /// </summary>
        public int MaxAllocatable { get; set; }

        /// <summary>
        /// Validation message with details.
        /// </summary>
        public string? Message { get; set; }
    }

    /// <summary>
    /// Represents a recommendation for product replenishment.
    /// </summary>
    public class ReplenishmentRecommendation
    {
        /// <summary>
        /// Product requiring replenishment.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name for display purposes.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Current stock level.
        /// </summary>
        public StockQuantity CurrentStock { get; set; }

        /// <summary>
        /// Minimum stock level threshold.
        /// </summary>
        public StockQuantity MinimumStock { get; set; }

        /// <summary>
        /// Recommended replenishment quantity.
        /// </summary>
        public int RecommendedQuantity { get; set; }

        /// <summary>
        /// Priority level for replenishment.
        /// </summary>
        public ReplenishmentPriority Priority { get; set; }

        /// <summary>
        /// Reason for the replenishment recommendation.
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Enumeration defining replenishment priority levels.
    /// </summary>
    public enum ReplenishmentPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}