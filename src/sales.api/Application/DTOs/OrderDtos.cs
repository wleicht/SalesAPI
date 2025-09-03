using SalesApi.Domain.ValueObjects;

namespace SalesApi.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object representing an order in the application layer.
    /// Provides a serializable representation of order data optimized for API communication
    /// and cross-boundary data transfer scenarios in the sales domain.
    /// </summary>
    /// <remarks>
    /// DTO Design Principles:
    /// 
    /// Boundary Isolation:
    /// - Decouples internal domain models from external contracts
    /// - Provides stable API contracts independent of domain changes
    /// - Enables versioning and backward compatibility
    /// - Optimized for serialization and network transfer
    /// 
    /// Data Projection:
    /// - Contains only necessary data for specific use cases
    /// - Flattened structure for simplified consumption
    /// - Performance optimized with minimal data transfer
    /// - Clear data contracts for API documentation
    /// 
    /// Cross-Layer Communication:
    /// - Safe data transfer between application layers
    /// - JSON-friendly structure for REST APIs
    /// - Compatible with various serialization frameworks
    /// - Supports caching and distributed scenarios
    /// 
    /// The DTO serves as the primary data contract for order information
    /// in API responses, inter-service communication, and client applications.
    /// </remarks>
    public class OrderDto
    {
        /// <summary>
        /// Unique identifier of the order.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the customer who placed the order.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Current status of the order in its lifecycle.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Total monetary amount for the complete order.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Currency code for the order amount.
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Collection of items included in the order.
        /// </summary>
        public List<OrderItemDto> Items { get; set; } = new();

        /// <summary>
        /// UTC timestamp when the order was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC timestamp when the order was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Identifier of the user who created the order.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the user who last updated the order.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data Transfer Object representing an order item in the application layer.
    /// Contains product information, quantities, and pricing details for order line items.
    /// </summary>
    /// <remarks>
    /// Order Item DTO Design:
    /// - Simplified structure for easy consumption
    /// - Contains essential item information
    /// - Supports order composition and display
    /// - Performance optimized for collections
    /// </remarks>
    public class OrderItemDto
    {
        /// <summary>
        /// Identifier of the product being ordered.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Name of the product at the time of order creation.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Quantity of the product being ordered.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Price per unit of the product.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total price for this line item.
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Currency code for the pricing information.
        /// </summary>
        public string Currency { get; set; } = "USD";
    }

    /// <summary>
    /// Data Transfer Object for order creation requests.
    /// Optimized for API input validation and order creation workflows.
    /// </summary>
    /// <remarks>
    /// Creation DTO Design:
    /// - Input validation friendly structure
    /// - Clear separation from domain models
    /// - API request optimization
    /// - Validation attribute support
    /// </remarks>
    public class CreateOrderDto
    {
        /// <summary>
        /// Identifier of the customer placing the order.
        /// </summary>
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Collection of items to include in the order.
        /// </summary>
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Data Transfer Object for order item creation requests.
    /// Contains product reference and quantity information for new order items.
    /// </summary>
    public class CreateOrderItemDto
    {
        /// <summary>
        /// Identifier of the product to order.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity of the product to order.
        /// </summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for order update requests.
    /// Supports partial order modifications and status updates.
    /// </summary>
    /// <remarks>
    /// Update DTO Design:
    /// - Flexible update scenarios
    /// - Partial update support
    /// - Status transition tracking
    /// - Audit information inclusion
    /// </remarks>
    public class UpdateOrderDto
    {
        /// <summary>
        /// New status for the order (optional).
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Updated items collection (optional).
        /// </summary>
        public List<UpdateOrderItemDto>? Items { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for order item updates.
    /// Supports quantity modifications and item management.
    /// </summary>
    public class UpdateOrderItemDto
    {
        /// <summary>
        /// Identifier of the product to update.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// New quantity for the product.
        /// </summary>
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for order statistics and metrics.
    /// Provides aggregated order data for reporting and analytics scenarios.
    /// </summary>
    /// <remarks>
    /// Statistics DTO Design:
    /// - Aggregated data representation
    /// - Performance optimized for reporting
    /// - Business intelligence support
    /// - Time-series data structure
    /// </remarks>
    public class OrderStatisticsDto
    {
        /// <summary>
        /// Period for which statistics are calculated.
        /// </summary>
        public DateTime Period { get; set; }

        /// <summary>
        /// Total number of orders in the period.
        /// </summary>
        public int TotalOrders { get; set; }

        /// <summary>
        /// Total order value in the period.
        /// </summary>
        public decimal TotalValue { get; set; }

        /// <summary>
        /// Average order value in the period.
        /// </summary>
        public decimal AverageOrderValue { get; set; }

        /// <summary>
        /// Number of confirmed orders in the period.
        /// </summary>
        public int ConfirmedOrders { get; set; }

        /// <summary>
        /// Number of cancelled orders in the period.
        /// </summary>
        public int CancelledOrders { get; set; }

        /// <summary>
        /// Number of fulfilled orders in the period.
        /// </summary>
        public int FulfilledOrders { get; set; }

        /// <summary>
        /// Order fulfillment rate as a percentage.
        /// </summary>
        public decimal FulfillmentRate { get; set; }

        /// <summary>
        /// Order cancellation rate as a percentage.
        /// </summary>
        public decimal CancellationRate { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for paginated order results.
    /// Provides pagination metadata along with order data.
    /// </summary>
    /// <remarks>
    /// Pagination DTO Design:
    /// - Standard pagination pattern
    /// - Metadata for UI pagination
    /// - Performance optimization
    /// - Consistent pagination interface
    /// </remarks>
    public class PagedOrderResultDto
    {
        /// <summary>
        /// Collection of orders for the current page.
        /// </summary>
        public List<OrderDto> Orders { get; set; } = new();

        /// <summary>
        /// Current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of orders per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of orders across all pages.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages available.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Indicates whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Indicates whether there is a next page.
        /// </summary>
        public bool HasNextPage { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for order operation results.
    /// Provides standardized response format for order operations.
    /// </summary>
    /// <remarks>
    /// Result DTO Design:
    /// - Standardized operation response
    /// - Success/failure indication
    /// - Error handling support
    /// - Consistent API response format
    /// </remarks>
    public class OrderOperationResultDto
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Order data if operation was successful.
        /// </summary>
        public OrderDto? Order { get; set; }

        /// <summary>
        /// Error message if operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Error code for specific error scenarios.
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Additional validation errors if applicable.
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();

        /// <summary>
        /// Creates a successful operation result.
        /// </summary>
        public static OrderOperationResultDto Success(OrderDto order) => 
            new() { IsSuccess = true, Order = order };

        /// <summary>
        /// Creates a failed operation result.
        /// </summary>
        public static OrderOperationResultDto Failure(string errorMessage, string? errorCode = null) => 
            new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };

        /// <summary>
        /// Creates a validation failure result.
        /// </summary>
        public static OrderOperationResultDto ValidationFailure(IEnumerable<string> errors) => 
            new() { IsSuccess = false, ValidationErrors = errors.ToList() };
    }
}