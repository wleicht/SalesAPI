using SalesApi.Domain.Entities;
using SalesApi.Domain.ValueObjects;
using SalesApi.Application.DTOs;
using MediatR;

namespace SalesApi.Application.Queries
{
    /// <summary>
    /// Represents a query to retrieve an order by its unique identifier.
    /// Provides flexible loading options for different use case scenarios.
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
    /// - Optional eager loading based on requirements
    /// - Projection capabilities for minimal data transfer
    /// - Caching-friendly query patterns
    /// - Efficient database access strategies
    /// 
    /// Use Case Alignment:
    /// - Supports various order display scenarios
    /// - Flexible loading for performance optimization
    /// - Clear data access patterns
    /// - Consistent query interface design
    /// </remarks>
    public record GetOrderByIdQuery : IRequest<OrderDto?>
    {
        /// <summary>
        /// Unique identifier of the order to retrieve.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// Indicates whether to include order items in the result.
        /// Enables performance optimization for scenarios not requiring item details.
        /// </summary>
        public bool IncludeItems { get; init; } = true;
    }

    /// <summary>
    /// Represents a query to retrieve orders for a specific customer with pagination.
    /// Supports customer order history display and customer service scenarios.
    /// </summary>
    /// <remarks>
    /// Customer Order Queries:
    /// - Filtered by customer for data isolation
    /// - Paginated for large order histories
    /// - Ordered by creation date for relevance
    /// - Supports customer service workflows
    /// </remarks>
    public record GetOrdersByCustomerQuery : IRequest<PagedOrderResultDto>
    {
        /// <summary>
        /// Unique identifier of the customer whose orders to retrieve.
        /// </summary>
        public Guid CustomerId { get; init; }

        /// <summary>
        /// Page number for pagination (1-based).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of orders per page.
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// Optional status filter for order state-specific queries.
        /// </summary>
        public string? Status { get; init; }
    }

    /// <summary>
    /// Represents a query to retrieve orders by status for operational reporting.
    /// Supports business operations requiring status-based order management.
    /// </summary>
    /// <remarks>
    /// Status-Based Queries:
    /// - Operational dashboards and monitoring
    /// - Business process management
    /// - Order pipeline visibility
    /// - Performance metrics and analytics
    /// </remarks>
    public record GetOrdersByStatusQuery : IRequest<PagedOrderResultDto>
    {
        /// <summary>
        /// Order status to filter by.
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Page number for pagination (1-based).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of orders per page.
        /// </summary>
        public int PageSize { get; init; } = 50;
    }

    /// <summary>
    /// Represents a query to retrieve orders within a specific date range.
    /// Supports reporting, analytics, and business intelligence scenarios.
    /// </summary>
    /// <remarks>
    /// Date Range Queries:
    /// - Business reporting and analytics
    /// - Financial period analysis
    /// - Performance trending and forecasting
    /// - Compliance and audit reporting
    /// </remarks>
    public record GetOrdersByDateRangeQuery : IRequest<PagedOrderResultDto>
    {
        /// <summary>
        /// Start date for the range (inclusive).
        /// </summary>
        public DateTime FromDate { get; init; }

        /// <summary>
        /// End date for the range (inclusive).
        /// </summary>
        public DateTime ToDate { get; init; }

        /// <summary>
        /// Optional status filter for refined results.
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Page number for pagination (1-based).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of orders per page.
        /// </summary>
        public int PageSize { get; init; } = 100;
    }

    /// <summary>
    /// Represents a query to retrieve order statistics and metrics.
    /// Supports business intelligence and performance monitoring scenarios.
    /// </summary>
    /// <remarks>
    /// Statistical Queries:
    /// - Business performance monitoring
    /// - Key performance indicator calculation
    /// - Trend analysis and forecasting
    /// - Executive reporting and dashboards
    /// </remarks>
    public record GetOrderStatisticsQuery : IRequest<List<OrderStatisticsDto>>
    {
        /// <summary>
        /// Start date for statistics calculation.
        /// </summary>
        public DateTime FromDate { get; init; }

        /// <summary>
        /// End date for statistics calculation.
        /// </summary>
        public DateTime ToDate { get; init; }

        /// <summary>
        /// Optional customer filter for customer-specific statistics.
        /// </summary>
        public Guid? CustomerId { get; init; }

        /// <summary>
        /// Grouping period for time-based statistics.
        /// </summary>
        public StatisticsPeriod GroupBy { get; init; } = StatisticsPeriod.Day;
    }

    /// <summary>
    /// Represents a query to get all orders with pagination.
    /// Supports general order management and administrative scenarios.
    /// </summary>
    public record GetOrdersQuery : IRequest<PagedOrderResultDto>
    {
        /// <summary>
        /// Page number for pagination (1-based).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of orders per page.
        /// </summary>
        public int PageSize { get; init; } = 20;
    }

    /// <summary>
    /// Enumeration for statistics grouping periods.
    /// </summary>
    public enum StatisticsPeriod
    {
        Hour,
        Day,
        Week,
        Month,
        Quarter,
        Year
    }
}