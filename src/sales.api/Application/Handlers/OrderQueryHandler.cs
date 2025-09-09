using SalesApi.Application.Queries;
using SalesApi.Application.DTOs;
using SalesApi.Domain.Entities;
using SalesApi.Domain.Repositories;
using Microsoft.Extensions.Logging;
using MediatR;

namespace SalesApi.Application.Handlers
{
    /// <summary>
    /// MediatR Request Handler responsible for handling order-related queries.
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
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetOrderByIdQueryHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the GetOrderByIdQueryHandler.
        /// </summary>
        /// <param name="orderRepository">Repository for order data access</param>
        /// <param name="logger">Logger for operation monitoring and troubleshooting</param>
        public GetOrderByIdQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetOrderByIdQueryHandler> logger)
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
        public async Task<OrderDto?> Handle(
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
                        "? Order not found | OrderId: {OrderId}",
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
                    OrderId = item.OrderId,
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

    /// <summary>
    /// MediatR Request Handler for retrieving orders with pagination.
    /// </summary>
    public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedOrderResultDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetOrdersQueryHandler> _logger;

        public GetOrdersQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetOrdersQueryHandler> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<PagedOrderResultDto> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "?? Retrieving orders | Page: {Page} | PageSize: {PageSize}",
                query.PageNumber, query.PageSize);

            try
            {
                var orders = await _orderRepository.GetPagedAsync(
                    query.PageNumber, 
                    query.PageSize, 
                    cancellationToken);

                var orderList = orders.ToList();
                var totalCount = await _orderRepository.CountAsync(cancellationToken);

                _logger.LogInformation(
                    "? Orders retrieved | Count: {Count} | Total: {Total}",
                    orderList.Count, totalCount);

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
                    "?? Error retrieving orders | Page: {Page}",
                    query.PageNumber);
                
                throw;
            }
        }

        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD",
                Items = order.Items.Select(item => new OrderItemDto
                {
                    OrderId = item.OrderId,
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

    /// <summary>
    /// MediatR Request Handler for retrieving orders by customer.
    /// </summary>
    public class GetOrdersByCustomerQueryHandler : IRequestHandler<GetOrdersByCustomerQuery, PagedOrderResultDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetOrdersByCustomerQueryHandler> _logger;

        public GetOrdersByCustomerQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetOrdersByCustomerQueryHandler> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<PagedOrderResultDto> Handle(GetOrdersByCustomerQuery query, CancellationToken cancellationToken)
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

        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD",
                Items = order.Items.Select(item => new OrderItemDto
                {
                    OrderId = item.OrderId,
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

    /// <summary>
    /// MediatR Request Handler for retrieving orders by status.
    /// </summary>
    public class GetOrdersByStatusQueryHandler : IRequestHandler<GetOrdersByStatusQuery, PagedOrderResultDto>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<GetOrdersByStatusQueryHandler> _logger;

        public GetOrdersByStatusQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetOrdersByStatusQueryHandler> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<PagedOrderResultDto> Handle(GetOrdersByStatusQuery query, CancellationToken cancellationToken)
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

        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Currency = "USD",
                Items = order.Items.Select(item => new OrderItemDto
                {
                    OrderId = item.OrderId,
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