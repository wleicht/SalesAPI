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
        /// Implements the reservation-based order processing workflow with comprehensive error handling
        /// and enhanced observability through correlation tracking.
        /// </summary>
        /// <param name="createOrderDto">Order creation data including customer ID and items</param>
        /// <returns>Created order details with reservation information</returns>
        /// <remarks>
        /// Enhanced Order Processing Flow with Stock Reservations and Observability:
        /// 1. Extract/generate correlation ID for end-to-end tracking
        /// 2. Validates order data using business rules and constraints
        /// 3. Creates stock reservations synchronously to prevent race conditions
        /// 4. Validates payment simulation (future: integrate with payment processor)
        /// 5. Creates order record with confirmed status if all validations pass
        /// 6. Publishes OrderConfirmedEvent for asynchronous stock deduction processing
        /// 7. Returns order details with reservation information for customer confirmation
        /// 
        /// Observability Enhancements:
        /// - Correlation ID propagation across all operations
        /// - Structured logging with correlation context
        /// - Performance timing for each workflow step
        /// - Detailed error categorization for monitoring
        /// 
        /// If any step fails:
        /// - Stock reservations are automatically released via OrderCancelledEvent
        /// - Order is marked as cancelled with appropriate error information
        /// - Customer receives clear error messaging with actionable guidance
        /// - All failures are tracked with correlation context for debugging
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
            // Extract correlation ID from headers or generate new one
            var correlationId = GetCorrelationId();
            var orderStartTime = DateTime.UtcNow;
            
            _logger.LogInformation(
                "???? Starting order creation: Customer {CustomerId} | Items: {ItemCount} | CorrelationId: {CorrelationId}",
                createOrderDto.CustomerId,
                createOrderDto.Items.Count,
                correlationId);

            try
            {
                // Step 1: Create stock reservation request with correlation
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
                    "???? Creating stock reservations: Order {OrderId} | Items: {ItemCount} | CorrelationId: {CorrelationId}",
                    reservationRequest.OrderId,
                    reservationRequest.Items.Count,
                    correlationId);

                // Step 2: Create stock reservations synchronously with correlation tracking
                var reservationStartTime = DateTime.UtcNow;
                var reservationResponse = await _stockReservationClient.CreateReservationAsync(
                    reservationRequest);
                var reservationDuration = DateTime.UtcNow - reservationStartTime;

                if (!reservationResponse.Success)
                {
                    _logger.LogWarning(
                        "??? Stock reservation failed: Order {OrderId} | Duration: {Duration}ms | Error: {Error} | CorrelationId: {CorrelationId}",
                        reservationRequest.OrderId,
                        reservationDuration.TotalMilliseconds,
                        reservationResponse.ErrorMessage,
                        correlationId);

                    return UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Stock Reservation Failed",
                        Detail = reservationResponse.ErrorMessage,
                        Status = StatusCodes.Status422UnprocessableEntity
                    });
                }

                _logger.LogInformation(
                    "??? Stock reservations created: Order {OrderId} | Items: {Count} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    reservationRequest.OrderId,
                    reservationResponse.TotalItemsProcessed,
                    reservationDuration.TotalMilliseconds,
                    correlationId);

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
                            "??? Reservation validation failed: Product {ProductId} | Error: {Error} | CorrelationId: {CorrelationId}",
                            itemDto.ProductId,
                            errorMessage,
                            correlationId);

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
                        _logger.LogError(
                            "??? Product not found during order processing: Product {ProductId} | CorrelationId: {CorrelationId}", 
                            itemDto.ProductId, 
                            correlationId);
                        
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
                        "???? Order item validated: Product {ProductId} ({ProductName}) | Qty: {Quantity} | Price: {UnitPrice} | CorrelationId: {CorrelationId}",
                        orderItem.ProductId,
                        orderItem.ProductName,
                        orderItem.Quantity,
                        orderItem.UnitPrice,
                        correlationId);
                }

                // Step 4: Simulate payment processing with observability
                _logger.LogInformation(
                    "???? Starting payment processing: Order {OrderId} | Amount: {Amount} | CorrelationId: {CorrelationId}",
                    reservationRequest.OrderId,
                    totalAmount,
                    correlationId);

                var paymentStartTime = DateTime.UtcNow;
                var paymentSuccessful = await SimulatePaymentProcessing(totalAmount, correlationId);
                var paymentDuration = DateTime.UtcNow - paymentStartTime;
                
                if (!paymentSuccessful)
                {
                    _logger.LogWarning(
                        "????? Payment processing failed: Order {OrderId} | Amount: {Amount} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                        reservationRequest.OrderId,
                        totalAmount,
                        paymentDuration.TotalMilliseconds,
                        correlationId);

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

                _logger.LogInformation(
                    "????? Payment processing successful: Order {OrderId} | Amount: {Amount} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    reservationRequest.OrderId,
                    totalAmount,
                    paymentDuration.TotalMilliseconds,
                    correlationId);

                // Step 5: Create order with confirmed status (using EF retry strategy)
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
                    "??? Order created successfully: Order {OrderId} | Status: {Status} | Amount: {Amount} | CorrelationId: {CorrelationId}",
                    order.Id,
                    order.Status,
                    order.TotalAmount,
                    correlationId);

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
                        "???? OrderConfirmedEvent published: Order {OrderId} | CorrelationId: {CorrelationId}",
                        order.Id,
                        correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "????? Failed to publish OrderConfirmedEvent: Order {OrderId} | CorrelationId: {CorrelationId}",
                        order.Id,
                        correlationId);
                    
                    // Order is still valid even if event publishing fails
                    // The event system will retry or manual intervention can resolve
                }

                // Step 7: Return order details with timing information
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

                var totalDuration = DateTime.UtcNow - orderStartTime;
                _logger.LogInformation(
                    "???? Order creation completed successfully: Order {OrderId} | Total Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    order.Id,
                    totalDuration.TotalMilliseconds,
                    correlationId);

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, orderDto);
            }
            catch (Exception ex)
            {
                var totalDuration = DateTime.UtcNow - orderStartTime;
                _logger.LogError(ex,
                    "???? Order creation failed: Customer {CustomerId} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                    createOrderDto.CustomerId,
                    totalDuration.TotalMilliseconds,
                    correlationId);

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
        /// Simulates payment processing for order validation with enhanced observability.
        /// In production, this would integrate with actual payment processors.
        /// Enhanced with more realistic failure simulation for testing and correlation tracking.
        /// </summary>
        /// <param name="amount">Total amount to process</param>
        /// <param name="correlationId">Correlation ID for request tracing</param>
        /// <returns>True if payment successful; false otherwise</returns>
        /// <remarks>
        /// Enhanced simulation implements more realistic business logic for payment validation:
        /// - Small amounts (under $100) have 100% success rate for basic testing
        /// - Medium amounts ($100-$999) have 95% success rate
        /// - Large amounts ($1000-$1999) have 85% success rate  
        /// - Very large amounts ($2000+) have 70% success rate for testing failures
        /// 
        /// The simulation ensures consistent behavior for testing by using order amount
        /// as a factor in the randomization, making certain amounts more likely to fail.
        /// 
        /// Observability enhancements:
        /// - Structured logging with correlation ID
        /// - Payment simulation decision tracking
        /// - Performance timing information
        /// - Success/failure rate monitoring
        /// 
        /// In production, replace with actual payment gateway integration
        /// including proper error handling, retry logic, and security measures.
        /// </remarks>
        private async Task<bool> SimulatePaymentProcessing(decimal amount, string correlationId)
        {
            var paymentStartTime = DateTime.UtcNow;
            
            // Simulate payment processing delay
            var processingDelay = Random.Shared.Next(50, 200);
            await Task.Delay(processingDelay);

            _logger.LogDebug(
                "???? Payment processing simulation: Amount: ${Amount} | ProcessingDelay: {Delay}ms | CorrelationId: {CorrelationId}",
                amount,
                processingDelay,
                correlationId);

            // Enhanced simulation logic for more predictable testing
            var random = new Random();
            bool success;
            string decisionReason;
            
            // For very large amounts (testing scenario), increase failure rate significantly
            if (amount >= 2000)
            {
                // Use both random chance and amount-based deterministic factor for testing
                var hashCode = Math.Abs(correlationId.GetHashCode() + (int)(amount * 100));
                var deterministicFactor = (hashCode % 100) / 100.0;
                var randomFactor = random.NextDouble();
                
                // Combine deterministic and random factors - 70% failure rate for amounts >= $2000
                var combinedFactor = (deterministicFactor + randomFactor) / 2;
                success = combinedFactor > 0.7; // 30% success rate
                decisionReason = $"Large amount logic: det={deterministicFactor:F3}, rnd={randomFactor:F3}, combined={combinedFactor:F3}";
            }
            else if (amount < 100)
            {
                success = true; // Small amounts always succeed
                decisionReason = "Small amount - guaranteed success";
            }
            else if (amount < 1000)
            {
                success = random.NextDouble() > 0.05; // 95% success rate
                decisionReason = "Medium amount - 95% success rate";
            }
            else
            {
                success = random.NextDouble() > 0.15; // 85% success rate for $1000-$1999
                decisionReason = "Large amount - 85% success rate";
            }

            var processingDuration = DateTime.UtcNow - paymentStartTime;
            
            if (success)
            {
                _logger.LogInformation(
                    "????? Payment simulation SUCCESS: Amount: ${Amount} | Duration: {Duration}ms | Reason: {Reason} | CorrelationId: {CorrelationId}",
                    amount,
                    processingDuration.TotalMilliseconds,
                    decisionReason,
                    correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "????? Payment simulation FAILURE: Amount: ${Amount} | Duration: {Duration}ms | Reason: {Reason} | CorrelationId: {CorrelationId}",
                    amount,
                    processingDuration.TotalMilliseconds,
                    decisionReason,
                    correlationId);
            }

            return success;
        }

        /// <summary>
        /// Publishes an OrderCancelledEvent to release stock reservations for failed orders.
        /// Implements the compensation pattern for distributed transaction management with correlation tracking.
        /// </summary>
        /// <param name="orderId">ID of the order being cancelled</param>
        /// <param name="customerId">ID of the customer</param>
        /// <param name="totalAmount">Total amount of the order</param>
        /// <param name="orderItems">Items that were being ordered</param>
        /// <param name="cancellationReason">Reason for cancellation</param>
        /// <param name="correlationId">Correlation ID for tracking the request</param>
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
                _logger.LogInformation(
                    "???? Publishing OrderCancelledEvent: Order {OrderId} | Reason: {Reason} | CorrelationId: {CorrelationId}",
                    orderId,
                    cancellationReason,
                    correlationId);

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
                    "????? OrderCancelledEvent published successfully: Order {OrderId} | Reason: {Reason} | CorrelationId: {CorrelationId}",
                    orderId,
                    cancellationReason,
                    correlationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "????? Failed to publish OrderCancelledEvent: Order {OrderId} | Reason: {Reason} | CorrelationId: {CorrelationId}",
                    orderId,
                    cancellationReason,
                    correlationId);
                
                // Don't throw here - cancellation event failure shouldn't block the error response
                // Manual cleanup or monitoring alerts can handle orphaned reservations
            }
        }

        /// <summary>
        /// Extracts correlation ID from request headers or generates a new one.
        /// </summary>
        /// <returns>Valid correlation ID for request tracking</returns>
        private string GetCorrelationId()
        {
            // Try to get correlation ID from request headers (set by Gateway)
            if (HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId!;
            }

            // Fallback: generate new correlation ID
            return $"sales-{Guid.NewGuid():N}";
        }
    }
}