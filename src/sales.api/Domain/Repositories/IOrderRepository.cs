using SalesApi.Domain.Entities;

namespace SalesApi.Domain.Repositories
{
    /// <summary>
    /// Defines the contract for Order entity persistence operations within the sales domain.
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
    /// - Methods aligned with business operations and use cases
    /// - Rich query capabilities for domain-specific scenarios
    /// - Performance-optimized operations for common business patterns
    /// - Support for complex business queries and reporting needs
    /// 
    /// Consistency and Transactions:
    /// - Aggregate root persistence with proper consistency boundaries
    /// - Transaction support for complex business operations
    /// - Optimistic concurrency control for multi-user scenarios
    /// - Atomic operations across related entities
    /// 
    /// The interface design follows Domain-Driven Design repository patterns
    /// while providing practical capabilities for production applications.
    /// </remarks>
    public interface IOrderRepository
    {
        /// <summary>
        /// Retrieves an order by its unique identifier with basic entity data.
        /// Optimized for scenarios where order items are not immediately required.
        /// </summary>
        /// <param name="orderId">Unique identifier of the order to retrieve</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Order entity if found, null otherwise</returns>
        /// <remarks>
        /// Performance Characteristics:
        /// - Lightweight query without related entities
        /// - Suitable for existence checks and basic operations
        /// - Does not load order items collection
        /// - Optimized for fast lookup scenarios
        /// 
        /// Use Cases:
        /// - Order status validation
        /// - Basic order information display
        /// - Existence verification before operations
        /// - Performance-critical scenarios requiring minimal data
        /// </remarks>
        Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an order with its complete item collection for comprehensive operations.
        /// Optimized for scenarios requiring full order context and item manipulation.
        /// </summary>
        /// <param name="orderId">Unique identifier of the order to retrieve</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Complete order entity with items if found, null otherwise</returns>
        /// <remarks>
        /// Performance Characteristics:
        /// - Eager loading of order items collection
        /// - Single database query with joins
        /// - Complete object graph for business operations
        /// - Optimized for modification scenarios
        /// 
        /// Use Cases:
        /// - Order modification operations (add/remove items)
        /// - Complete order display and reporting
        /// - Business logic requiring full order context
        /// - Order processing and fulfillment workflows
        /// </remarks>
        Task<Order?> GetWithItemsAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all orders for a specific customer with pagination support.
        /// Enables customer service operations and customer-specific reporting.
        /// </summary>
        /// <param name="customerId">Identifier of the customer</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of orders per page</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated collection of customer orders</returns>
        /// <remarks>
        /// Query Characteristics:
        /// - Ordered by creation date (most recent first)
        /// - Includes basic order information without items
        /// - Supports large customer order histories
        /// - Efficient pagination for user interface scenarios
        /// 
        /// Business Applications:
        /// - Customer order history display
        /// - Customer service order lookup
        /// - Customer analytics and behavior analysis
        /// - Order tracking and status monitoring
        /// </remarks>
        Task<IEnumerable<Order>> GetByCustomerAsync(
            Guid customerId, 
            int pageNumber = 1, 
            int pageSize = 50, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves orders filtered by status with pagination for operational reporting.
        /// Supports business operations requiring status-based order management.
        /// </summary>
        /// <param name="status">Order status to filter by</param>
        /// <param name="pageNumber">Page number for pagination (1-based)</param>
        /// <param name="pageSize">Number of orders per page</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated collection of orders with specified status</returns>
        /// <remarks>
        /// Operational Use Cases:
        /// - Pending orders requiring processing
        /// - Confirmed orders ready for fulfillment
        /// - Cancelled orders requiring cleanup
        /// - Fulfilled orders for completion reporting
        /// 
        /// Query Optimization:
        /// - Database index on status column for performance
        /// - Ordered by creation date for processing priority
        /// - Lightweight queries for operational dashboards
        /// - Supports high-volume operational scenarios
        /// </remarks>
        Task<IEnumerable<Order>> GetByStatusAsync(
            string status, 
            int pageNumber = 1, 
            int pageSize = 100, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves orders created within a specific date range for reporting and analytics.
        /// Supports business intelligence, financial reporting, and operational analysis.
        /// </summary>
        /// <param name="fromDate">Start date for the range (inclusive)</param>
        /// <param name="toDate">End date for the range (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of orders within the specified date range</returns>
        /// <remarks>
        /// Analytics Applications:
        /// - Daily, weekly, monthly sales reporting
        /// - Financial period analysis and reconciliation
        /// - Business performance metrics and KPIs
        /// - Seasonal trend analysis and forecasting
        /// 
        /// Performance Considerations:
        /// - Date range queries optimized with database indexes
        /// - Reasonable date ranges to prevent performance issues
        /// - Consider pagination for large date ranges
        /// - Support for timezone-aware date filtering
        /// </remarks>
        Task<IEnumerable<Order>> GetByDateRangeAsync(
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches orders by customer and status combination for targeted operations.
        /// Enables customer service scenarios requiring specific status filtering.
        /// </summary>
        /// <param name="customerId">Identifier of the customer</param>
        /// <param name="status">Order status to filter by</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of customer orders with specified status</returns>
        /// <remarks>
        /// Customer Service Use Cases:
        /// - Customer pending orders requiring attention
        /// - Customer confirmed orders for status updates
        /// - Customer cancelled orders for refund processing
        /// - Customer fulfilled orders for support scenarios
        /// 
        /// Query Efficiency:
        /// - Composite index on customer ID and status
        /// - Limited result set for focused operations
        /// - Ordered by creation date for relevance
        /// - Optimized for customer service workflows
        /// </remarks>
        Task<IEnumerable<Order>> GetByCustomerAndStatusAsync(
            Guid customerId, 
            string status, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new order to the repository with proper validation and consistency.
        /// Handles all aspects of order persistence including audit trail creation.
        /// </summary>
        /// <param name="order">Order entity to persist</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Persisted order entity with generated database values</returns>
        /// <remarks>
        /// Persistence Behavior:
        /// - Validates order entity and business rules
        /// - Persists order and all related items atomically
        /// - Updates audit fields and timestamps
        /// - Generates any database-assigned values
        /// 
        /// Transaction Handling:
        /// - Operates within existing transaction if available
        /// - Creates new transaction if none exists
        /// - Ensures atomic persistence of order and items
        /// - Handles optimistic concurrency conflicts
        /// 
        /// Validation:
        /// - Domain rules validation before persistence
        /// - Database constraint validation
        /// - Foreign key relationship validation
        /// - Business rule compliance verification
        /// </remarks>
        Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing order with optimistic concurrency control.
        /// Handles order modifications while maintaining data consistency and audit trails.
        /// </summary>
        /// <param name="order">Modified order entity to update</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Updated order entity with refreshed database values</returns>
        /// <remarks>
        /// Update Behavior:
        /// - Validates order modifications against business rules
        /// - Updates order and related items atomically
        /// - Refreshes audit fields and timestamps
        /// - Handles optimistic concurrency conflicts
        /// 
        /// Concurrency Control:
        /// - Uses entity version or timestamp for conflict detection
        /// - Throws exception on concurrent modification conflicts
        /// - Supports retry scenarios for transient conflicts
        /// - Maintains data integrity in multi-user scenarios
        /// 
        /// Modification Scope:
        /// - Updates order properties and status
        /// - Manages order items collection changes
        /// - Preserves audit trail and historical data
        /// - Validates business rule compliance
        /// </remarks>
        Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an order from the repository with proper cleanup and validation.
        /// Handles soft deletion scenarios and maintains referential integrity.
        /// </summary>
        /// <param name="orderId">Unique identifier of the order to remove</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if order was removed, false if not found</returns>
        /// <remarks>
        /// Deletion Strategy:
        /// - Consider soft deletion for audit compliance
        /// - Handle related entity cleanup (order items)
        /// - Validate deletion business rules
        /// - Maintain referential integrity
        /// 
        /// Business Rules:
        /// - Prevent deletion of fulfilled orders
        /// - Allow deletion of cancelled orders
        /// - Consider audit and compliance requirements
        /// - Handle cascade deletion of related entities
        /// 
        /// Alternative Approaches:
        /// - Soft deletion with deleted flag
        /// - Archive to separate table for history
        /// - Status change to "Deleted" instead of removal
        /// - Retention policies for compliance requirements
        /// </remarks>
        Task<bool> DeleteAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an order exists in the repository without loading the full entity.
        /// Optimized for existence validation scenarios requiring minimal resource usage.
        /// </summary>
        /// <param name="orderId">Unique identifier of the order to check</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>True if order exists, false otherwise</returns>
        /// <remarks>
        /// Performance Optimization:
        /// - Database query without entity materialization
        /// - Minimal network traffic and memory usage
        /// - Optimized for high-frequency validation scenarios
        /// - Suitable for caching and validation workflows
        /// 
        /// Use Cases:
        /// - Order ID validation before operations
        /// - Duplicate order prevention
        /// - Quick existence checks in business rules
        /// - Performance-critical validation scenarios
        /// </remarks>
        Task<bool> ExistsAsync(Guid orderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts the total number of orders for a specific customer.
        /// Provides customer analytics and business intelligence data.
        /// </summary>
        /// <param name="customerId">Identifier of the customer</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Total number of orders for the customer</returns>
        /// <remarks>
        /// Analytics Applications:
        /// - Customer loyalty and engagement metrics
        /// - Customer segmentation and classification
        /// - Customer lifetime value calculations
        /// - Business intelligence dashboards
        /// 
        /// Query Optimization:
        /// - Database count query without entity loading
        /// - Index optimization for customer-based counting
        /// - Suitable for real-time analytics scenarios
        /// - Supports high-volume customer analytics
        /// </remarks>
        Task<int> CountByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts orders by status for operational monitoring and reporting.
        /// Provides operational insights and business process monitoring.
        /// </summary>
        /// <param name="status">Order status to count</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Number of orders with the specified status</returns>
        /// <remarks>
        /// Operational Monitoring:
        /// - Order pipeline health and flow monitoring
        /// - Business process efficiency metrics
        /// - Operational dashboard key performance indicators
        /// - Alert thresholds for business process issues
        /// 
        /// Real-time Applications:
        /// - Live operational dashboards
        /// - Business process monitoring alerts
        /// - Capacity planning and resource allocation
        /// - Performance trend analysis and optimization
        /// </remarks>
        Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken = default);
    }
}