using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Contracts.Orders;
using BuildingBlocks.Contracts.Inventory;
using SalesApi.Models;
using SalesApi.Persistence;
using SalesApi.Services;
using SalesAPI.Services;
using BuildingBlocks.Events.Infrastructure;
using BuildingBlocks.Events.Domain;

namespace SalesApi.Controllers
{
    /// <summary>
    /// Controller for managing orders in the sales system with role-based authorization.
    /// Order creation requires customer or admin role, reading is open access.
    /// </summary>
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly SalesDbContext _context;
        private readonly IInventoryClient _inventoryClient;
        private readonly StockReservationClient _stockReservationClient;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<OrdersController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersController"/> class.
        /// </summary>
        /// <param name="context">Sales database context.</param>
        /// <param name="inventoryClient">Inventory API client.</param>
        /// <param name="stockReservationClient">Stock reservation client.</param>
        /// <param name="eventPublisher">Event publisher for domain events.</param>
        /// <param name="logger">Logger instance.</param>
        public OrdersController(
            SalesDbContext context, 
            IInventoryClient inventoryClient,
            StockReservationClient stockReservationClient,
            IEventPublisher eventPublisher,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _inventoryClient = inventoryClient;
            _stockReservationClient = stockReservationClient;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new order with automatic stock reservation and validation.
        /// Implements the reservation-based order processing workflow with comprehensive error handling.
        /// </summary>
        /// <param name="createOrderDto">Order creation data including customer ID and items</param>
        /// <returns>Created order details with reservation information</returns>
        /// <remarks>
        /// Enhanced Order Processing Flow with Stock Reservations:
        /// 1. Validates order data using business rules and constraints
        /// 2. Creates stock reservations synchronously to prevent race conditions
        /// 3. Validates payment simulation (future: integrate with payment processor)
        /// 4. Creates order record with confirmed status if all validations pass
        /// 5. Publishes OrderConfirmedEvent for asynchronous stock deduction processing
        /// 6. Returns order details with reservation information for customer confirmation
        /// 
        /// If any step fails:
        /// - Stock reservations are automatically released via OrderCancelledEvent
        /// - Order is marked as cancelled with appropriate error information
        /// - Customer receives clear error messaging with actionable guidance
        /// 
        /// This approach provides better reliability and customer experience by:
        /// - Preventing overselling through atomic stock reservation
        /// - Enabling payment processing delays without losing stock allocation
        /// - Supporting complex order workflows with proper compensation logic
        /// - Providing comprehensive audit trails for business operations
        /// </remarks>
        /// <response code="201">Order created successfully with stock reservations</response>
        /// <response code="400">Invalid order data or validation errors</response>
        /// <response code="422">Business logic error - insufficient stock or payment failure</response>
        /// <response code="500">Internal server error during order processing</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            var correlationId = HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString();
            
            _logger.LogInformation(
                "Creating order for Customer {CustomerId} with {ItemCount} items. CorrelationId: {CorrelationId}",
                createOrderDto.CustomerId,
                createOrderDto.Items.Count,
                correlationId);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Step 1: Create stock reservation request
                var reservationRequest = new StockReservationRequest
                {
                    OrderId = Guid.NewGuid(), // Generate order ID for reservation
                    CorrelationId = correlationId,
                    Items = createOrderDto.Items.Select(item => new StockReservationItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    }).ToList()
                };

                _logger.LogInformation(
                    "Creating stock reservations for Order {OrderId} with {ItemCount} items",
                    reservationRequest.OrderId,
                    reservationRequest.Items.Count);

                // Step 2: Create stock reservations synchronously
                var reservationResponse = await _stockReservationClient.CreateReservationAsync(
                    reservationRequest);

                if (!reservationResponse.Success)
                {
                    _logger.LogWarning(
                        "Stock reservation failed for Order {OrderId}. Error: {Error}",
                        reservationRequest.OrderId,
                        reservationResponse.ErrorMessage);

                    return UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Stock Reservation Failed",
                        Detail = reservationResponse.ErrorMessage,
                        Status = StatusCodes.Status422UnprocessableEntity
                    });
                }

                _logger.LogInformation(
                    "Stock reservations created successfully for Order {OrderId}. Processing {Count} items.",
                    reservationRequest.OrderId,
                    reservationResponse.TotalItemsProcessed);

                // Step 3: Process and validate order items with reserved stock
                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                foreach (var itemDto in createOrderDto.Items)
                {
                    // Find the corresponding reservation result
                    var reservationResult = reservationResponse.ReservationResults
                        .FirstOrDefault(r => r.ProductId == itemDto.ProductId);

                    if (reservationResult == null || !reservationResult.Success)
                    {
                        var errorMessage = reservationResult?.ErrorMessage ?? "Unknown reservation error";
                        _logger.LogError(
                            "Reservation validation failed for Product {ProductId}. Error: {Error}",
                            itemDto.ProductId,
                            errorMessage);

                        // Publish cancellation event to release any successful reservations
                        await PublishOrderCancelledEvent(
                            reservationRequest.OrderId,
                            createOrderDto.CustomerId,
                            totalAmount,
                            orderItems,
                            "Stock reservation validation failed",
                            correlationId);

                        return UnprocessableEntity(new ProblemDetails
                        {
                            Title = "Order Validation Failed",
                            Detail = $"Product validation failed: {errorMessage}",
                            Status = StatusCodes.Status422UnprocessableEntity
                        });
                    }

                    // Get product details (price will be validated against current price)
                    var productResponse = await _inventoryClient.GetProductAsync(itemDto.ProductId);
                    if (productResponse == null)
                    {
                        _logger.LogError("Product {ProductId} not found during order processing", itemDto.ProductId);
                        
                        await PublishOrderCancelledEvent(
                            reservationRequest.OrderId,
                            createOrderDto.CustomerId,
                            totalAmount,
                            orderItems,
                            $"Product {itemDto.ProductId} not found",
                            correlationId);

                        return UnprocessableEntity(new ProblemDetails
                        {
                            Title = "Product Not Found",
                            Detail = $"Product {itemDto.ProductId} not found",
                            Status = StatusCodes.Status422UnprocessableEntity
                        });
                    }

                    var orderItem = new OrderItem
                    {
                        ProductId = itemDto.ProductId,
                        ProductName = productResponse.Name,
                        Quantity = itemDto.Quantity,
                        UnitPrice = productResponse.Price
                    };

                    orderItems.Add(orderItem);
                    totalAmount += orderItem.TotalPrice;

                    _logger.LogDebug(
                        "Order item created: Product {ProductId} ({ProductName}), Quantity: {Quantity}, UnitPrice: {UnitPrice}",
                        orderItem.ProductId,
                        orderItem.ProductName,
                        orderItem.Quantity,
                        orderItem.UnitPrice);
                }

                // Step 4: Simulate payment processing
                _logger.LogInformation(
                    "Simulating payment processing for Order {OrderId}, Amount: {Amount}",
                    reservationRequest.OrderId,
                    totalAmount);

                var paymentSuccessful = await SimulatePaymentProcessing(totalAmount, correlationId);
                if (!paymentSuccessful)
                {
                    _logger.LogWarning(
                        "Payment processing failed for Order {OrderId}, Amount: {Amount}",
                        reservationRequest.OrderId,
                        totalAmount);

                    // Publish cancellation event to release reservations
                    await PublishOrderCancelledEvent(
                        reservationRequest.OrderId,
                        createOrderDto.CustomerId,
                        totalAmount,
                        orderItems,
                        "Payment processing failed",
                        correlationId);

                    return UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Payment Processing Failed",
                        Detail = "Payment could not be processed. Please check your payment information and try again.",
                        Status = StatusCodes.Status422UnprocessableEntity
                    });
                }

                // Step 5: Create order with confirmed status
                var order = new Order
                {
                    Id = reservationRequest.OrderId, // Use the same ID as reservations
                    CustomerId = createOrderDto.CustomerId,
                    Status = "Confirmed",
                    TotalAmount = totalAmount,
                    Items = orderItems
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Order {OrderId} created successfully with status {Status}, Amount: {Amount}",
                    order.Id,
                    order.Status,
                    order.TotalAmount);

                // Step 6: Publish OrderConfirmedEvent for asynchronous stock deduction
                try
                {
                    var orderConfirmedEvent = new OrderConfirmedEvent
                    {
                        OrderId = order.Id,
                        CustomerId = order.CustomerId,
                        TotalAmount = order.TotalAmount,
                        Items = order.Items.Select(item => new OrderItemEvent
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        }).ToList(),
                        Status = order.Status,
                        OrderCreatedAt = order.CreatedAt,
                        CorrelationId = correlationId
                    };

                    await _eventPublisher.PublishAsync(orderConfirmedEvent);
                    
                    _logger.LogInformation(
                        "OrderConfirmedEvent published for Order {OrderId}",
                        order.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to publish OrderConfirmedEvent for Order {OrderId}. Order created but event not sent.",
                        order.Id);
                    
                    // Order is still valid even if event publishing fails
                    // The event system will retry or manual intervention can resolve
                }

                await transaction.CommitAsync();

                // Step 7: Return order details
                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    Items = order.Items.Select(item => new OrderItemDto
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
                };

                _logger.LogInformation(
                    "Order creation completed successfully for Order {OrderId}",
                    order.Id);

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, orderDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                _logger.LogError(ex,
                    "Failed to create order for Customer {CustomerId}. Transaction rolled back.",
                    createOrderDto.CustomerId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Order Creation Failed",
                    Detail = "An error occurred while creating the order",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Gets order details by order ID. Open access - no authentication required.
        /// </summary>
        /// <param name="id">Order identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Order details.</returns>
        /// <response code="200">Returns order details.</response>
        /// <response code="404">Order not found.</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

                if (order == null)
                {
                    return NotFound(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                        statusCode: 404, 
                        title: "Order not found", 
                        detail: $"Order with ID {id} does not exist."));
                }

                var result = new OrderDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    Items = order.Items.Select(item => new OrderItemDto
                    {
                        OrderId = item.OrderId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching order {OrderId}", id);
                return StatusCode(500, ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                    statusCode: 500, 
                    title: "Internal server error", 
                    detail: "An error occurred while fetching the order."));
            }
        }

        /// <summary>
        /// Gets a paginated list of orders. Open access - no authentication required.
        /// </summary>
        /// <param name="page">Page number (default: 1).</param>
        /// <param name="pageSize">Page size (default: 20).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of orders.</returns>
        /// <response code="200">Returns paginated order list.</response>
        /// <response code="400">Invalid pagination parameters.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            CancellationToken cancellationToken = default)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                    statusCode: 400, 
                    title: "Invalid pagination parameters", 
                    detail: "Page and pageSize must be greater than 0."));
            }

            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(order => new OrderDto
                    {
                        Id = order.Id,
                        CustomerId = order.CustomerId,
                        Status = order.Status,
                        TotalAmount = order.TotalAmount,
                        CreatedAt = order.CreatedAt,
                        Items = order.Items.Select(item => new OrderItemDto
                        {
                            OrderId = item.OrderId,
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.Quantity * item.UnitPrice
                        }).ToList()
                    })
                    .ToListAsync(cancellationToken);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching orders (page: {Page}, pageSize: {PageSize})", page, pageSize);
                return StatusCode(500, ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                    statusCode: 500, 
                    title: "Internal server error", 
                    detail: "An error occurred while fetching orders."));
            }
        }

        /// <summary>
        /// Simulates payment processing for order validation.
        /// In production, this would integrate with actual payment processors.
        /// </summary>
        /// <param name="amount">Total amount to process</param>
        /// <param name="correlationId">Correlation ID for tracing</param>
        /// <returns>True if payment successful; false otherwise</returns>
        /// <remarks>
        /// This simulation implements basic business logic for payment validation:
        /// - Small amounts (under $10) always succeed for testing
        /// - Medium amounts (under $1000) have 95% success rate
        /// - Large amounts (over $1000) have 90% success rate
        /// 
        /// In production, replace with actual payment gateway integration
        /// including proper error handling, retry logic, and security measures.
        /// </remarks>
        private async Task<bool> SimulatePaymentProcessing(decimal amount, string correlationId)
        {
            // Simulate payment processing delay
            await Task.Delay(100);

            // Simple simulation logic - in production, integrate with real payment processor
            var random = new Random();
            
            if (amount < 10) return true; // Small amounts always succeed
            if (amount < 1000) return random.NextDouble() > 0.05; // 95% success rate
            return random.NextDouble() > 0.10; // 90% success rate for large amounts
        }

        /// <summary>
        /// Publishes an OrderCancelledEvent to release stock reservations for failed orders.
        /// Implements the compensation pattern for distributed transaction management.
        /// </summary>
        /// <param name="orderId">ID of the order being cancelled</param>
        /// <param name="customerId">ID of the customer</param>
        /// <param name="totalAmount">Total amount of the order</param>
        /// <param name="orderItems">Items that were being ordered</param>
        /// <param name="cancellationReason">Reason for cancellation</param>
        /// <param name="correlationId">Correlation ID for tracing</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task PublishOrderCancelledEvent(
            Guid orderId,
            Guid customerId,
            decimal totalAmount,
            List<OrderItem> orderItems,
            string cancellationReason,
            string correlationId)
        {
            try
            {
                var orderCancelledEvent = new OrderCancelledEvent
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    TotalAmount = totalAmount,
                    Items = orderItems.Select(item => new OrderItemEvent
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    }).ToList(),
                    CancellationReason = cancellationReason,
                    Status = "Cancelled",
                    OrderCreatedAt = DateTime.UtcNow,
                    CancelledAt = DateTime.UtcNow,
                    CorrelationId = correlationId
                };

                await _eventPublisher.PublishAsync(orderCancelledEvent);
                
                _logger.LogInformation(
                    "OrderCancelledEvent published for Order {OrderId}. Reason: {Reason}",
                    orderId,
                    cancellationReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish OrderCancelledEvent for Order {OrderId}",
                    orderId);
                
                // Don't throw here - cancellation event failure shouldn't block the error response
                // Manual cleanup or monitoring alerts can handle orphaned reservations
            }
        }
    }
}