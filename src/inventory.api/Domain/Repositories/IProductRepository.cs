using InventoryApi.Domain.Entities;
using InventoryApi.Domain.ValueObjects;

namespace InventoryApi.Domain.Repositories
{
    /// <summary>
    /// Defines the contract for Product entity persistence operations within the inventory domain.
    /// Provides comprehensive data access capabilities while maintaining domain-driven design
    /// principles and separation between domain logic and infrastructure concerns.
    /// </summary>
    /// <remarks>
    /// Repository Pattern Benefits:
    /// 
    /// Domain Independence:
    /// - Abstracts persistence technology from domain logic
    /// - Enables testing with in-memory or mock implementations
    /// - Supports multiple storage backends without domain changes
    /// - Maintains clean architecture layer separation
    /// 
    /// Business-Focused Interface:
    /// - Methods aligned with inventory operations and use cases
    /// - Rich query capabilities for inventory-specific scenarios
    /// - Performance-optimized operations for common inventory patterns
    /// - Support for complex inventory queries and reporting needs
    /// 
    /// Consistency and Transactions:
    /// - Product aggregate persistence with proper consistency boundaries
    /// - Transaction support for complex inventory operations
    /// - Optimistic concurrency control for multi-user scenarios
    /// - Atomic operations for stock management
    /// 
    /// The interface design follows Domain-Driven Design repository patterns
    /// while providing practical capabilities for production inventory systems.
    /// </remarks>
    public interface IProductRepository
    {
        /// <summary>
        /// Retrieves a product by its unique identifier with complete entity data.
        /// Optimized for scenarios requiring full product information and stock details.
        /// </summary>
        /// <param name="productId">Unique identifier of the product to retrieve</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Product entity if found, null otherwise</returns>
        /// <remarks>
        /// Performance Characteristics:
        /// - Single database query for complete product data
        /// - Includes all product properties and stock information
        /// - Suitable for business operations requiring full context
        /// - Optimized for modification and business logic scenarios
        /// 
        /// Use Cases:
        /// - Product detail display and editing
        /// - Stock management operations
        /// - Business logic requiring complete product context
        /// - Inventory operations and adjustments
        /// </remarks>
        Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves products by their identifiers in batch for efficient operations.
        /// Optimized for scenarios requiring multiple products with minimal database queries.
        /// </summary>
        /// <param name="productIds">Collection of product identifiers to retrieve</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of found products (may be fewer than requested if some not found)</returns>
        /// <remarks>
        /// Batch Retrieval Benefits:
        /// - Single database query for multiple products
        /// - Efficient network utilization and connection usage
        /// - Suitable for order processing and bulk operations
        /// - Maintains consistent data snapshot across products
        /// 
        /// Use Cases:
        /// - Order validation with multiple products
        /// - Bulk inventory operations and reports
        /// - Shopping cart and order processing
        /// - Batch product updates and synchronization
        /// </remarks>
        Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all active products with pagination support for catalog management.
        /// Enables product browsing, catalog operations, and administrative functions.
        /// </summary>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of products per page</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated collection of active products</returns>
        /// <remarks>
        /// Active Product Criteria:
        /// - Products with IsActive flag set to true
        /// - Available for customer ordering and catalog display
        /// - Excludes discontinued and inactive products
        /// - Ordered by creation date or other business criteria
        /// 
        /// Pagination Benefits:
        /// - Efficient handling of large product catalogs
        /// - Consistent user interface performance
        /// - Reduced memory usage and network transfer
        /// - Support for infinite scroll and page-based navigation
        /// </remarks>
        Task<IEnumerable<Product>> GetActiveProductsAsync(
            int pageNumber = 1, 
            int pageSize = 50, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves products with low stock levels for replenishment planning.
        /// Supports proactive inventory management and automated procurement workflows.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of products with stock at or below minimum levels</returns>
        /// <remarks>
        /// Low Stock Criteria:
        /// - Current stock quantity at or below minimum stock level
        /// - Active products that require replenishment attention
        /// - Excludes inactive products from replenishment alerts
        /// - Ordered by severity of stock shortage
        /// 
        /// Business Applications:
        /// - Procurement planning and purchase order generation
        /// - Inventory manager dashboards and alerts
        /// - Automated replenishment system triggers
        /// - Supplier notification and coordination
        /// </remarks>
        Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves products that are currently out of stock for immediate attention.
        /// Supports customer service, sales planning, and urgent replenishment scenarios.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of products with zero stock quantity</returns>
        /// <remarks>
        /// Out of Stock Criteria:
        /// - Products with stock quantity of zero
        /// - May include both active and inactive products
        /// - Critical for customer service and sales operations
        /// - Requires immediate attention for customer satisfaction
        /// 
        /// Operational Uses:
        /// - Customer service out-of-stock notifications
        /// - Sales team product availability updates
        /// - Urgent procurement and expedited ordering
        /// - Website and catalog availability updates
        /// </remarks>
        Task<IEnumerable<Product>> GetOutOfStockProductsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches products by name with partial matching for catalog browsing.
        /// Supports customer search functionality and product discovery workflows.
        /// </summary>
        /// <param name="searchTerm">Partial name to search for</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of products per page</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated collection of products matching search criteria</returns>
        /// <remarks>
        /// Search Characteristics:
        /// - Case-insensitive partial name matching
        /// - Supports wildcards and fuzzy matching where available
        /// - Limited to active products for customer-facing scenarios
        /// - Ordered by relevance and product popularity
        /// 
        /// Search Applications:
        /// - Customer product search and discovery
        /// - Administrative product management
        /// - Inventory lookup and identification
        /// - Customer service product assistance
        /// </remarks>
        Task<IEnumerable<Product>> SearchByNameAsync(
            string searchTerm, 
            int pageNumber = 1, 
            int pageSize = 50, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if sufficient stock is available for the requested quantity without loading entities.
        /// Optimized for high-frequency availability checks and order validation scenarios.
        /// </summary>
        /// <param name="productId">Product identifier to check</param>
        /// <param name="requestedQuantity">Quantity needed for operation</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if sufficient stock is available, false otherwise</returns>
        /// <remarks>
        /// Performance Optimization:
        /// - Database query without entity materialization
        /// - Minimal network traffic and memory usage
        /// - Optimized for high-frequency validation scenarios
        /// - Suitable for real-time availability checks
        /// 
        /// Availability Rules:
        /// - Product must be active for availability
        /// - Stock quantity must meet or exceed requested amount
        /// - Considers only currently available (non-reserved) stock
        /// - Fast response for user interface scenarios
        /// 
        /// Use Cases:
        /// - Real-time cart validation
        /// - Order feasibility checks
        /// - Product availability indicators
        /// - Inventory constraint validation
        /// </remarks>
        Task<bool> IsStockAvailableAsync(
            Guid productId, 
            int requestedQuantity, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new product to the inventory with proper validation and consistency.
        /// Handles all aspects of product creation including audit trail establishment.
        /// </summary>
        /// <param name="product">Product entity to persist</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Persisted product entity with generated database values</returns>
        /// <remarks>
        /// Creation Process:
        /// - Validates product entity and business rules
        /// - Generates unique identifiers and timestamps
        /// - Establishes audit trail and creation metadata
        /// - Ensures all required fields are properly set
        /// 
        /// Business Rule Validation:
        /// - Product name uniqueness within active products
        /// - Valid price and stock quantity values
        /// - Proper category and classification assignment
        /// - Compliance with business constraints and policies
        /// 
        /// Transaction Handling:
        /// - Operates within existing transaction if available
        /// - Creates new transaction if none exists
        /// - Ensures atomic creation with all related data
        /// - Handles constraint violations and conflicts gracefully
        /// </remarks>
        Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing product with optimistic concurrency control.
        /// Handles product modifications while maintaining data consistency and audit trails.
        /// </summary>
        /// <param name="product">Modified product entity to update</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Updated product entity with refreshed database values</returns>
        /// <remarks>
        /// Update Behavior:
        /// - Validates product modifications against business rules
        /// - Updates all changed properties atomically
        /// - Refreshes audit fields and timestamps
        /// - Handles optimistic concurrency conflicts
        /// 
        /// Concurrency Control:
        /// - Uses entity version or timestamp for conflict detection
        /// - Throws exception on concurrent modification conflicts
        /// - Supports retry scenarios for transient conflicts
        /// - Maintains data integrity in multi-user scenarios
        /// 
        /// Change Tracking:
        /// - Identifies modified properties for audit purposes
        /// - Preserves historical data and change tracking
        /// - Updates search indexes and related systems
        /// - Triggers domain events for significant changes
        /// </remarks>
        Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a product from the inventory with proper cleanup and validation.
        /// Handles soft deletion scenarios and maintains referential integrity.
        /// </summary>
        /// <param name="productId">Unique identifier of the product to remove</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if product was removed, false if not found</returns>
        /// <remarks>
        /// Deletion Strategy:
        /// - Consider soft deletion for audit compliance
        /// - Validate deletion business rules and constraints
        /// - Handle related entity cleanup and references
        /// - Maintain referential integrity across system
        /// 
        /// Business Rules:
        /// - Prevent deletion of products with pending orders
        /// - Allow deletion of discontinued products without stock
        /// - Consider audit and compliance requirements
        /// - Handle catalog and search index updates
        /// 
        /// Alternative Approaches:
        /// - Soft deletion with IsActive flag
        /// - Archive to separate table for history
        /// - Status change to "Discontinued" instead of removal
        /// - Retention policies for compliance requirements
        /// </remarks>
        Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a product exists in the repository without loading the full entity.
        /// Optimized for existence validation scenarios requiring minimal resource usage.
        /// </summary>
        /// <param name="productId">Unique identifier of the product to check</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if product exists, false otherwise</returns>
        /// <remarks>
        /// Performance Optimization:
        /// - Database query without entity materialization
        /// - Minimal network traffic and memory usage
        /// - Optimized for high-frequency validation scenarios
        /// - Suitable for caching and validation workflows
        /// 
        /// Use Cases:
        /// - Product ID validation before operations
        /// - Duplicate product prevention
        /// - Quick existence checks in business rules
        /// - Performance-critical validation scenarios
        /// </remarks>
        Task<bool> ExistsAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the total number of active products in the inventory.
        /// Provides inventory metrics and business intelligence data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Total number of active products</returns>
        /// <remarks>
        /// Counting Scope:
        /// - Includes only active products available for sale
        /// - Excludes discontinued and inactive products
        /// - Provides real-time inventory size metrics
        /// - Supports business intelligence and reporting
        /// 
        /// Use Cases:
        /// - Inventory management dashboards
        /// - Business metrics and KPI reporting
        /// - Catalog size monitoring and planning
        /// - Capacity planning and resource allocation
        /// </remarks>
        Task<int> CountActiveProductsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts products that are currently below minimum stock levels.
        /// Provides inventory health metrics and replenishment planning data.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Number of products with low stock conditions</returns>
        /// <remarks>
        /// Low Stock Metrics:
        /// - Products with stock at or below minimum levels
        /// - Critical for inventory health monitoring
        /// - Supports automated alerting and notifications
        /// - Enables proactive replenishment planning
        /// 
        /// Business Applications:
        /// - Inventory health dashboards
        /// - Procurement planning metrics
        /// - Supply chain performance monitoring
        /// - Risk assessment and mitigation
        /// </remarks>
        Task<int> CountLowStockProductsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates the total value of inventory based on current stock and prices.
        /// Provides financial metrics for inventory valuation and reporting.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Total monetary value of current inventory</returns>
        /// <remarks>
        /// Valuation Calculation:
        /// - Sum of (stock quantity × current price) for all active products
        /// - Uses current selling prices for valuation
        /// - Excludes inactive and discontinued products
        /// - Provides snapshot of inventory investment
        /// 
        /// Financial Applications:
        /// - Balance sheet inventory valuation
        /// - Financial reporting and analysis
        /// - Investment tracking and ROI calculation
        /// - Insurance coverage and risk assessment
        /// </remarks>
        Task<decimal> CalculateTotalInventoryValueAsync(CancellationToken cancellationToken = default);
    }
}