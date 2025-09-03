using InventoryApi.Domain.ValueObjects;

namespace InventoryApi.Application.Queries
{
    /// <summary>
    /// Represents a query to retrieve a product by its unique identifier.
    /// Provides complete product information for display and management scenarios.
    /// </summary>
    /// <remarks>
    /// Query Design Principles:
    /// 
    /// CQRS Implementation:
    /// - Read-only operations separated from commands
    /// - Optimized for data retrieval and display
    /// - No side effects or state changes
    /// - Clear separation of concerns
    /// 
    /// Performance Optimization:
    /// - Single entity retrieval for efficiency
    /// - Complete product data for comprehensive display
    /// - Caching-friendly query patterns
    /// - Efficient database access strategies
    /// </remarks>
    public record GetProductByIdQuery
    {
        /// <summary>
        /// Unique identifier of the product to retrieve.
        /// </summary>
        public Guid ProductId { get; init; }
    }

    /// <summary>
    /// Represents a query to retrieve multiple products by their identifiers.
    /// Supports batch operations and order processing scenarios.
    /// </summary>
    public record GetProductsByIdsQuery
    {
        /// <summary>
        /// Collection of product identifiers to retrieve.
        /// </summary>
        public List<Guid> ProductIds { get; init; } = new();
    }

    /// <summary>
    /// Represents a query to retrieve all active products with pagination.
    /// Supports catalog browsing and product management scenarios.
    /// </summary>
    /// <remarks>
    /// Active Product Queries:
    /// - Filtered by active status for customer-facing scenarios
    /// - Paginated for large product catalogs
    /// - Ordered by relevance or business criteria
    /// - Supports catalog management workflows
    /// </remarks>
    public record GetActiveProductsQuery
    {
        /// <summary>
        /// Page number for pagination (1-based).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of products per page.
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// Optional search term for product name filtering.
        /// </summary>
        public string? SearchTerm { get; init; }
    }

    /// <summary>
    /// Represents a query to retrieve products with low stock levels.
    /// Supports inventory management and replenishment planning.
    /// </summary>
    /// <remarks>
    /// Low Stock Queries:
    /// - Products below minimum stock thresholds
    /// - Critical for inventory health monitoring
    /// - Supports proactive replenishment planning
    /// - Enables automated alerting and notifications
    /// </remarks>
    public record GetLowStockProductsQuery
    {
        /// <summary>
        /// Optional filter to include only active products.
        /// </summary>
        public bool ActiveOnly { get; init; } = true;

        /// <summary>
        /// Optional threshold override for low stock definition.
        /// </summary>
        public int? CustomThreshold { get; init; }
    }

    /// <summary>
    /// Represents a query to retrieve products that are currently out of stock.
    /// Supports urgent inventory management and customer service scenarios.
    /// </summary>
    public record GetOutOfStockProductsQuery
    {
        /// <summary>
        /// Optional filter to include only active products.
        /// </summary>
        public bool ActiveOnly { get; init; } = true;
    }

    /// <summary>
    /// Represents a query to search products by name with flexible matching.
    /// Supports catalog search and product discovery workflows.
    /// </summary>
    /// <remarks>
    /// Search Capabilities:
    /// - Partial name matching for flexible search
    /// - Case-insensitive search for user convenience
    /// - Paginated results for large datasets
    /// - Relevance-based ordering where possible
    /// </remarks>
    public record SearchProductsQuery
    {
        /// <summary>
        /// Search term for product name matching.
        /// </summary>
        public string SearchTerm { get; init; } = string.Empty;

        /// <summary>
        /// Page number for pagination (1-based).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of products per page.
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// Optional filter to include only active products.
        /// </summary>
        public bool ActiveOnly { get; init; } = true;
    }

    /// <summary>
    /// Represents a query to check stock availability for multiple products.
    /// Supports order validation and availability checking scenarios.
    /// </summary>
    /// <remarks>
    /// Availability Checking:
    /// - Batch availability validation for efficiency
    /// - Supports order processing workflows
    /// - Real-time stock status information
    /// - Performance optimized for frequent checks
    /// </remarks>
    public record CheckStockAvailabilityQuery
    {
        /// <summary>
        /// Collection of stock requirements to validate.
        /// </summary>
        public List<StockAvailabilityRequest> StockRequirements { get; init; } = new();
    }

    /// <summary>
    /// Represents a stock availability request for a specific product.
    /// </summary>
    public record StockAvailabilityRequest
    {
        /// <summary>
        /// Product identifier to check availability for.
        /// </summary>
        public Guid ProductId { get; init; }

        /// <summary>
        /// Required quantity for the availability check.
        /// </summary>
        public int RequiredQuantity { get; init; }
    }

    /// <summary>
    /// Represents a query to retrieve inventory statistics and metrics.
    /// Supports business intelligence and operational reporting scenarios.
    /// </summary>
    /// <remarks>
    /// Statistical Queries:
    /// - Comprehensive inventory health metrics
    /// - Business performance indicators
    /// - Financial valuation and analysis
    /// - Operational efficiency measurements
    /// </remarks>
    public record GetInventoryStatisticsQuery
    {
        /// <summary>
        /// Optional filter to include only active products in statistics.
        /// </summary>
        public bool ActiveOnly { get; init; } = true;

        /// <summary>
        /// Optional category filter for targeted statistics.
        /// </summary>
        public string? Category { get; init; }
    }

    /// <summary>
    /// Represents a query to retrieve replenishment recommendations.
    /// Supports proactive inventory management and procurement planning.
    /// </summary>
    /// <remarks>
    /// Replenishment Planning:
    /// - Automated recommendation generation
    /// - Priority-based ordering for urgent needs
    /// - Configurable recommendation criteria
    /// - Integration with procurement systems
    /// </remarks>
    public record GetReplenishmentRecommendationsQuery
    {
        /// <summary>
        /// Minimum priority level for recommendations to include.
        /// </summary>
        public ReplenishmentPriority MinimumPriority { get; init; } = ReplenishmentPriority.Low;

        /// <summary>
        /// Maximum number of recommendations to return.
        /// </summary>
        public int MaxRecommendations { get; init; } = 50;

        /// <summary>
        /// Optional category filter for targeted recommendations.
        /// </summary>
        public string? Category { get; init; }
    }

    /// <summary>
    /// Represents a query to retrieve inventory value analysis.
    /// Supports financial reporting and asset management scenarios.
    /// </summary>
    public record GetInventoryValueAnalysisQuery
    {
        /// <summary>
        /// Optional date for historical value analysis.
        /// </summary>
        public DateTime? AsOfDate { get; init; }

        /// <summary>
        /// Optional category filter for targeted analysis.
        /// </summary>
        public string? Category { get; init; }

        /// <summary>
        /// Currency for value reporting.
        /// </summary>
        public string Currency { get; init; } = "USD";
    }

    /// <summary>
    /// Enumeration for replenishment priority levels.
    /// </summary>
    public enum ReplenishmentPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}