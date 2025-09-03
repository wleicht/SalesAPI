using InventoryApi.Domain.ValueObjects;

namespace InventoryApi.Application.DTOs
{
    /// <summary>
    /// Data Transfer Object representing a product in the application layer.
    /// Provides a serializable representation of product data optimized for API communication
    /// and cross-boundary data transfer scenarios in the inventory domain.
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
    /// </remarks>
    public class ProductDto
    {
        /// <summary>
        /// Unique identifier of the product.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the product for display and search.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the product.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Current selling price of the product.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Currency code for the product price.
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Current available stock quantity.
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// Indicates whether the product is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Minimum stock level that triggers replenishment alerts.
        /// </summary>
        public int MinimumStockLevel { get; set; }

        /// <summary>
        /// Indicates whether the product is currently low on stock.
        /// </summary>
        public bool IsLowStock { get; set; }

        /// <summary>
        /// Indicates whether the product is currently out of stock.
        /// </summary>
        public bool IsOutOfStock { get; set; }

        /// <summary>
        /// Indicates whether the product is available for ordering.
        /// </summary>
        public bool IsAvailableForOrder { get; set; }

        /// <summary>
        /// UTC timestamp when the product was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// UTC timestamp when the product was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Identifier of the user who created the product.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the user who last updated the product.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data Transfer Object for product creation requests.
    /// Optimized for API input validation and product creation workflows.
    /// </summary>
    public class CreateProductDto
    {
        /// <summary>
        /// Name of the product to create.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the product to create.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Initial price for the product.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Initial stock quantity for the product.
        /// </summary>
        public int InitialStock { get; set; }

        /// <summary>
        /// Minimum stock level for replenishment alerts.
        /// </summary>
        public int MinimumStockLevel { get; set; } = 10;
    }

    /// <summary>
    /// Data Transfer Object for product update requests.
    /// Supports partial product modifications and updates.
    /// </summary>
    public class UpdateProductDto
    {
        /// <summary>
        /// Updated name for the product (optional).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Updated description for the product (optional).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Updated price for the product (optional).
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Updated minimum stock level (optional).
        /// </summary>
        public int? MinimumStockLevel { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for stock adjustment requests.
    /// Supports inventory corrections and stock management operations.
    /// </summary>
    public class StockAdjustmentDto
    {
        /// <summary>
        /// Product identifier for the stock adjustment.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity to adjust (positive for addition, negative for subtraction).
        /// </summary>
        public int AdjustmentQuantity { get; set; }

        /// <summary>
        /// Reason for the stock adjustment.
        /// </summary>
        public string AdjustmentReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data Transfer Object for stock availability information.
    /// Provides availability status and quantity details for products.
    /// </summary>
    public class StockAvailabilityDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name for display purposes.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Current available stock quantity.
        /// </summary>
        public int AvailableQuantity { get; set; }

        /// <summary>
        /// Requested quantity for availability check.
        /// </summary>
        public int RequestedQuantity { get; set; }

        /// <summary>
        /// Indicates whether sufficient stock is available.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Maximum quantity that can be allocated.
        /// </summary>
        public int MaxAllocatable { get; set; }

        /// <summary>
        /// Status message about availability.
        /// </summary>
        public string? AvailabilityMessage { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for batch stock availability results.
    /// Provides comprehensive availability information for multiple products.
    /// </summary>
    public class BatchStockAvailabilityDto
    {
        /// <summary>
        /// Indicates whether all requested stock is available.
        /// </summary>
        public bool AllAvailable { get; set; }

        /// <summary>
        /// Individual availability results for each product.
        /// </summary>
        public List<StockAvailabilityDto> ProductAvailabilities { get; set; } = new();

        /// <summary>
        /// Summary message about the overall availability status.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// UTC timestamp when the availability check was performed.
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data Transfer Object for inventory statistics and metrics.
    /// Provides aggregated inventory data for reporting and analytics scenarios.
    /// </summary>
    public class InventoryStatisticsDto
    {
        /// <summary>
        /// Total number of products in the inventory.
        /// </summary>
        public int TotalProducts { get; set; }

        /// <summary>
        /// Number of active products available for sale.
        /// </summary>
        public int ActiveProducts { get; set; }

        /// <summary>
        /// Number of inactive or discontinued products.
        /// </summary>
        public int InactiveProducts { get; set; }

        /// <summary>
        /// Number of products with low stock levels.
        /// </summary>
        public int LowStockProducts { get; set; }

        /// <summary>
        /// Number of products that are out of stock.
        /// </summary>
        public int OutOfStockProducts { get; set; }

        /// <summary>
        /// Total inventory value based on current stock and prices.
        /// </summary>
        public decimal TotalInventoryValue { get; set; }

        /// <summary>
        /// Currency for the inventory value calculation.
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Average product price across all products.
        /// </summary>
        public decimal AverageProductPrice { get; set; }

        /// <summary>
        /// Total stock units across all products.
        /// </summary>
        public int TotalStockUnits { get; set; }

        /// <summary>
        /// Average stock level across all products.
        /// </summary>
        public decimal AverageStockLevel { get; set; }

        /// <summary>
        /// Percentage of products that are low on stock.
        /// </summary>
        public decimal LowStockPercentage { get; set; }

        /// <summary>
        /// Percentage of products that are out of stock.
        /// </summary>
        public decimal OutOfStockPercentage { get; set; }

        /// <summary>
        /// UTC timestamp when the statistics were calculated.
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Data Transfer Object for replenishment recommendations.
    /// Provides actionable insights for inventory procurement and planning.
    /// </summary>
    public class ReplenishmentRecommendationDto
    {
        /// <summary>
        /// Product requiring replenishment.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name for display and identification.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Current stock level.
        /// </summary>
        public int CurrentStock { get; set; }

        /// <summary>
        /// Minimum stock level threshold.
        /// </summary>
        public int MinimumStock { get; set; }

        /// <summary>
        /// Recommended quantity to replenish.
        /// </summary>
        public int RecommendedQuantity { get; set; }

        /// <summary>
        /// Priority level for the replenishment.
        /// </summary>
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// Reason for the replenishment recommendation.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Estimated cost for the recommended replenishment.
        /// </summary>
        public decimal EstimatedCost { get; set; }

        /// <summary>
        /// Days until stock-out based on current consumption patterns.
        /// </summary>
        public int? DaysUntilStockOut { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for paginated product results.
    /// Provides pagination metadata along with product data.
    /// </summary>
    public class PagedProductResultDto
    {
        /// <summary>
        /// Collection of products for the current page.
        /// </summary>
        public List<ProductDto> Products { get; set; } = new();

        /// <summary>
        /// Current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of products per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of products across all pages.
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
    /// Data Transfer Object for inventory operation results.
    /// Provides standardized response format for inventory operations.
    /// </summary>
    public class InventoryOperationResultDto
    {
        /// <summary>
        /// Indicates whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Product data if operation was successful.
        /// </summary>
        public ProductDto? Product { get; set; }

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
        public static InventoryOperationResultDto Success(ProductDto product) => 
            new() { IsSuccess = true, Product = product };

        /// <summary>
        /// Creates a failed operation result.
        /// </summary>
        public static InventoryOperationResultDto Failure(string errorMessage, string? errorCode = null) => 
            new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };

        /// <summary>
        /// Creates a validation failure result.
        /// </summary>
        public static InventoryOperationResultDto ValidationFailure(IEnumerable<string> errors) => 
            new() { IsSuccess = false, ValidationErrors = errors.ToList() };
    }

    /// <summary>
    /// Data Transfer Object for stock operation results.
    /// Provides detailed information about stock-related operations.
    /// </summary>
    public class StockOperationResultDto
    {
        /// <summary>
        /// Indicates whether the stock operation was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Product identifier for the operation.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Previous stock quantity before the operation.
        /// </summary>
        public int PreviousStock { get; set; }

        /// <summary>
        /// New stock quantity after the operation.
        /// </summary>
        public int NewStock { get; set; }

        /// <summary>
        /// Quantity that was affected by the operation.
        /// </summary>
        public int AffectedQuantity { get; set; }

        /// <summary>
        /// Type of stock operation performed.
        /// </summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the operation triggered a low stock alert.
        /// </summary>
        public bool LowStockTriggered { get; set; }

        /// <summary>
        /// Error message if operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// UTC timestamp when the operation was performed.
        /// </summary>
        public DateTime OperationTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a successful stock operation result.
        /// </summary>
        public static StockOperationResultDto Success(
            Guid productId,
            int previousStock,
            int newStock,
            int affectedQuantity,
            string operationType,
            bool lowStockTriggered = false) => 
            new() 
            { 
                IsSuccess = true,
                ProductId = productId,
                PreviousStock = previousStock,
                NewStock = newStock,
                AffectedQuantity = affectedQuantity,
                OperationType = operationType,
                LowStockTriggered = lowStockTriggered
            };

        /// <summary>
        /// Creates a failed stock operation result.
        /// </summary>
        public static StockOperationResultDto Failure(Guid productId, string errorMessage) => 
            new() 
            { 
                IsSuccess = false, 
                ProductId = productId, 
                ErrorMessage = errorMessage 
            };
    }
}