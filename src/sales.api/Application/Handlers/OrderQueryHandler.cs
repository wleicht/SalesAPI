using SalesApi.Application.Queries;
using SalesApi.Application.DTOs;
using SalesApi.Domain.Entities;
using SalesApi.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace SalesApi.Application.Handlers
{
    /// <summary>
    /// Application service responsible for handling order-related queries.
    /// Orchestrates query processing, data retrieval, and response formatting
    /// for order information requests in the sales domain.
    /// </summary>
    /// <remarks>
    /// Handler Responsibilities:
    /// 
    /// Query Processing:
    /// - Processes read-only data requests
    /// - Optimizes data retrieval for specific use cases
    /// - Handles pagination and filtering requirements
    /// - Manages query performance and caching strategies
    /// 
    /// Data Projection:
    /// - Maps domain entities to appropriate DTOs
    /// - Applies projections for performance optimization
    /// - Handles data aggregation and statistics
    /// - Supports multiple response formats
    /// 
    /// Cross-Cutting Concerns:
    /// - Logging and monitoring for query operations
    /// - Error handling and exception management
    /// - Performance tracking and optimization
    /// - Caching strategy implementation
    /// 
    /// The handler follows CQRS patterns with clear separation between
    /// command and query responsibilities for optimal performance and maintainability.
    /// </remarks>
    public class OrderQueryHandler
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderQueryHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the OrderQueryHandler.
        /// </summary>
        /// <param name="orderRepository">Repository for order data access</param>
        /// <param name="logger">Logger for operation monitoring and troubleshooting</param>
        public OrderQueryHandler(
            IOrderRepository orderRepository,
            ILogger<OrderQueryHandler> logger)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles retrieval of a single order by its unique identifier.
        /// Provides flexible loading options based on query requirements.
        /// </summary>
        /// <param name="query">Query containing order identifier and loading options</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Order DTO if found, null otherwise</returns>
        /// <remarks>
        /// Query Processing:
        /// 1. Validate query parameters
        /// 2. Determine loading strategy based on requirements
        /// 3. Retrieve order from repository
        /// 4. Map domain entity to DTO
        /// 5. Return formatted response
        /// 
        /// Performance Optimization:
        /// - Conditional loading of order items
        /// - Efficient database queries
        /// - Minimal data transfer
        /// - Logging for performance monitoring
        /// </remarks>
        public async Task<OrderDto?> HandleAsync(
            GetOrderByIdQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving order by ID | OrderId: {OrderId} | IncludeItems: {IncludeItems}",
                query.OrderId, query.IncludeItems);

            try
            {
                Order? order;

                if (query.IncludeItems)
                {
                    order = await _orderRepository.GetWithItemsAsync(query.OrderId, cancellationToken);
                }
                else
                {
                    order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
                }

                if (order == null)
                {
                    _logger.LogInformation(
                        "?? Order not found | OrderId: {OrderId}",
                        query.OrderId);
                    
                    return null;
                }

                _logger.LogInformation(
                    "? Order retrieved successfully | OrderId: {OrderId} | Status: {Status}",
                    order.Id, order.Status);

                return MapOrderToDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving order | OrderId: {OrderId}",
                    query.OrderId);
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of orders for a specific customer with pagination and filtering.
        /// Supports customer order history display and customer service scenarios.
        /// </summary>
        /// <param name="query">Query containing customer criteria and pagination options</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated result containing customer orders</returns>
        public async Task<PagedOrderResultDto> HandleAsync(
            GetOrdersByCustomerQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving orders by customer | CustomerId: {CustomerId} | Page: {Page} | Status: {Status}",
                query.CustomerId, query.PageNumber, query.Status);

            try
            {
                IEnumerable<Order> orders;

                if (!string.IsNullOrEmpty(query.Status))
                {
                    orders = await _orderRepository.GetByCustomerAndStatusAsync(
                        query.CustomerId, 
                        query.Status, 
                        cancellationToken);
                }
                else
                {
                    orders = await _orderRepository.GetByCustomerAsync(
                        query.CustomerId, 
                        query.PageNumber, 
                        query.PageSize, 
                        cancellationToken);
                }

                var orderList = orders.ToList();
                var totalCount = await _orderRepository.CountByCustomerAsync(query.CustomerId, cancellationToken);

                _logger.LogInformation(
                    "? Customer orders retrieved | CustomerId: {CustomerId} | Count: {Count} | Total: {Total}",
                    query.CustomerId, orderList.Count, totalCount);

                return new PagedOrderResultDto
                {
                    Orders = orderList.Select(MapOrderToDto).ToList(),
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                    HasPreviousPage = query.PageNumber > 1,
                    HasNextPage = query.PageNumber < Math.Ceiling((double)totalCount / query.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving customer orders | CustomerId: {CustomerId}",
                    query.CustomerId);
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of orders by status for operational reporting and monitoring.
        /// Supports business operations requiring status-based order management.
        /// </summary>
        /// <param name="query">Query containing status criteria and pagination options</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated result containing orders with specified status</returns>
        public async Task<PagedOrderResultDto> HandleAsync(
            GetOrdersByStatusQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving orders by status | Status: {Status} | Page: {Page}",
                query.Status, query.PageNumber);

            try
            {
                var orders = await _orderRepository.GetByStatusAsync(
                    query.Status, 
                    query.PageNumber, 
                    query.PageSize, 
                    cancellationToken);

                var orderList = orders.ToList();
                var totalCount = await _orderRepository.CountByStatusAsync(query.Status, cancellationToken);

                _logger.LogInformation(
                    "? Status orders retrieved | Status: {Status} | Count: {Count} | Total: {Total}",
                    query.Status, orderList.Count, totalCount);

                return new PagedOrderResultDto
                {
                    Orders = orderList.Select(MapOrderToDto).ToList(),
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                    HasPreviousPage = query.PageNumber > 1,
                    HasNextPage = query.PageNumber < Math.Ceiling((double)totalCount / query.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving orders by status | Status: {Status}",
                    query.Status);
                
                throw;
            }
        }

        /// <summary>
        /// Handles retrieval of orders within a specific date range for reporting and analytics.
        /// Supports business intelligence, financial reporting, and operational analysis.
        /// </summary>
        /// <param name="query">Query containing date range and filtering criteria</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Paginated result containing orders within the specified date range</returns>
        public async Task<PagedOrderResultDto> HandleAsync(
            GetOrdersByDateRangeQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Retrieving orders by date range | From: {FromDate} | To: {ToDate} | Status: {Status}",
                query.FromDate, query.ToDate, query.Status);

            try
            {
                var orders = await _orderRepository.GetByDateRangeAsync(
                    query.FromDate, 
                    query.ToDate, 
                    cancellationToken);

                var orderList = orders.ToList();

                // Apply status filter if specified
                if (!string.IsNullOrEmpty(query.Status))
                {
                    orderList = orderList.Where(o => o.Status == query.Status).ToList();
                }

                // Apply pagination
                var pagedOrders = orderList
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();

                _logger.LogInformation(
                    "? Date range orders retrieved | From: {FromDate} | To: {ToDate} | Count: {Count}",
                    query.FromDate, query.ToDate, pagedOrders.Count);

                return new PagedOrderResultDto
                {
                    Orders = pagedOrders.Select(MapOrderToDto).ToList(),
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = orderList.Count,
                    TotalPages = (int)Math.Ceiling((double)orderList.Count / query.PageSize),
                    HasPreviousPage = query.PageNumber > 1,
                    HasNextPage = query.PageNumber < Math.Ceiling((double)orderList.Count / query.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error retrieving orders by date range | From: {FromDate} | To: {ToDate}",
                    query.FromDate, query.ToDate);
                
                throw;
            }
        }

        /// <summary>
        /// Handles calculation and retrieval of order statistics and metrics.
        /// Supports business intelligence and performance monitoring scenarios.
        /// </summary>
        /// <param name="query">Query containing statistics criteria and grouping options</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Collection of order statistics grouped by specified period</returns>
        public async Task<List<OrderStatisticsDto>> HandleAsync(
            GetOrderStatisticsQuery query, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "?? Calculating order statistics | From: {FromDate} | To: {ToDate} | GroupBy: {GroupBy}",
                query.FromDate, query.ToDate, query.GroupBy);

            try
            {
                var orders = await _orderRepository.GetByDateRangeAsync(
                    query.FromDate, 
                    query.ToDate, 
                    cancellationToken);

                var orderList = orders.ToList();

                // Apply customer filter if specified
                if (query.CustomerId.HasValue)
                {
                    orderList = orderList.Where(o => o.CustomerId == query.CustomerId.Value).ToList();
                }

                // Group orders by the specified period
                var groupedOrders = GroupOrdersByPeriod(orderList, query.GroupBy);

                var statistics = groupedOrders.Select(group => CalculateStatistics(group.Key, group.ToList())).ToList();

                _logger.LogInformation(
                    "? Order statistics calculated | Periods: {PeriodCount} | Total Orders: {TotalOrders}",
                    statistics.Count, orderList.Count);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "?? Error calculating order statistics | From: {FromDate} | To: {ToDate}",
                    query.FromDate, query.ToDate);
                
                throw;
            }
        }

        /// <summary>
        /// Groups orders by the specified time period for statistics calculation.
        /// </summary>
        /// <param name="orders">Orders to group</param>
        /// <param name="period">Grouping period</param>
        /// <returns>Orders grouped by time period</returns>
        private static IEnumerable<IGrouping<DateTime, Order>> GroupOrdersByPeriod(
            List<Order> orders, 
            StatisticsPeriod period)
        {
            return period switch
            {
                StatisticsPeriod.Hour => orders.GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, o.CreatedAt.Day, o.CreatedAt.Hour, 0, 0)),
                StatisticsPeriod.Day => orders.GroupBy(o => o.CreatedAt.Date),
                StatisticsPeriod.Week => orders.GroupBy(o => GetWeekStart(o.CreatedAt)),
                StatisticsPeriod.Month => orders.GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, 1)),
                StatisticsPeriod.Quarter => orders.GroupBy(o => GetQuarterStart(o.CreatedAt)),
                StatisticsPeriod.Year => orders.GroupBy(o => new DateTime(o.CreatedAt.Year, 1, 1)),
                _ => orders.GroupBy(o => o.CreatedAt.Date)
            };
        }

        /// <summary>
        /// Gets the start date of the week for the specified date.
        /// </summary>
        private static DateTime GetWeekStart(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Gets the start date of the quarter for the specified date.
        /// </summary>
        private static DateTime GetQuarterStart(DateTime date)
        {
            var quarterNumber = (date.Month - 1) / 3 + 1;
            return new DateTime(date.Year, (quarterNumber - 1) * 3 + 1, 1);
        }

        /// <summary>
        /// Calculates statistics for a group of orders in a specific period.
        /// </summary>
        /// <param name="period">Time period for the statistics</param>
        /// <param name="orders">Orders in the period</param>
        /// <returns>Calculated statistics for the period</returns>
        private static OrderStatisticsDto CalculateStatistics(DateTime period, List<Order> orders)
        {
            var totalOrders = orders.Count;
            var confirmedOrders = orders.Count(o => o.Status == OrderStatus.Confirmed);
            var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
            var fulfilledOrders = orders.Count(o => o.Status == OrderStatus.Fulfilled);
            var totalValue = orders.Sum(o => o.TotalAmount);
            var averageOrderValue = totalOrders > 0 ? totalValue / totalOrders : 0;

            return new OrderStatisticsDto
            {
                Period = period,
                TotalOrders = totalOrders,
                TotalValue = totalValue,
                AverageOrderValue = averageOrderValue,
                ConfirmedOrders = confirmedOrders,
                CancelledOrders = cancelledOrders,
                FulfilledOrders = fulfilledOrders,
                FulfillmentRate = totalOrders > 0 ? (decimal)fulfilledOrders / totalOrders * 100 : 0,
                CancellationRate = totalOrders > 0 ? (decimal)cancelledOrders / totalOrders * 100 : 0
            };
        }

        /// <summary>
        /// Maps a domain Order entity to an OrderDto for API response.
        /// Provides clean separation between domain and application layers.
        /// </summary>
        /// <param name="order">Domain order entity</param>
        /// <returns>Order DTO for API response</returns>
        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD", // TODO: Get from domain or configuration
                Items = order.Items.Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Currency = "USD"
                }).ToList(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt ?? order.CreatedAt,
                CreatedBy = order.CreatedBy ?? string.Empty,
                UpdatedBy = order.UpdatedBy ?? order.CreatedBy ?? string.Empty
            };
        }
    }
}