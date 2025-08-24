using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Contracts.Orders;
using SalesApi.Models;
using SalesApi.Persistence;
using SalesApi.Services;

namespace SalesApi.Controllers
{
    /// <summary>
    /// Controller for managing orders in the sales system.
    /// </summary>
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly SalesDbContext _dbContext;
        private readonly IInventoryClient _inventoryClient;
        private readonly ILogger<OrdersController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersController"/> class.
        /// </summary>
        /// <param name="dbContext">Sales database context.</param>
        /// <param name="inventoryClient">Inventory API client.</param>
        /// <param name="logger">Logger instance.</param>
        public OrdersController(SalesDbContext dbContext, IInventoryClient inventoryClient, ILogger<OrdersController> logger)
        {
            _dbContext = dbContext;
            _inventoryClient = inventoryClient;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new order with stock validation.
        /// </summary>
        /// <param name="dto">Order creation data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Created order details.</returns>
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken cancellationToken = default)
        {
            // Check ModelState for Data Annotations validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Creating order for customer {CustomerId} with {ItemCount} items", 
                    dto.CustomerId, dto.Items.Count);

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = dto.CustomerId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    TotalAmount = 0
                };

                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                // Validate each item and calculate total
                foreach (var itemDto in dto.Items)
                {
                    _logger.LogDebug("Validating product {ProductId} for quantity {Quantity}", 
                        itemDto.ProductId, itemDto.Quantity);

                    // Fetch product from Inventory API
                    var product = await _inventoryClient.GetProductByIdAsync(itemDto.ProductId, cancellationToken);
                    
                    if (product == null)
                    {
                        _logger.LogWarning("Product {ProductId} not found", itemDto.ProductId);
                        return BadRequest(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                            statusCode: 400, 
                            title: "Product not found", 
                            detail: $"Product with ID {itemDto.ProductId} does not exist."));
                    }

                    // Validate stock quantity
                    if (product.StockQuantity < itemDto.Quantity)
                    {
                        _logger.LogWarning("Insufficient stock for product {ProductId}. Available: {Available}, Requested: {Requested}", 
                            itemDto.ProductId, product.StockQuantity, itemDto.Quantity);
                        
                        return UnprocessableEntity(ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                            statusCode: 422, 
                            title: "Insufficient stock", 
                            detail: $"Product '{product.Name}' has only {product.StockQuantity} units available, but {itemDto.Quantity} requested."));
                    }

                    // Create order item with frozen price
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = itemDto.ProductId,
                        ProductName = product.Name,
                        Quantity = itemDto.Quantity,
                        UnitPrice = product.Price
                    };

                    orderItems.Add(orderItem);
                    totalAmount += orderItem.Quantity * orderItem.UnitPrice;

                    _logger.LogDebug("Added item {ProductName} x{Quantity} at {UnitPrice} each", 
                        product.Name, itemDto.Quantity, product.Price);
                }

                // Update order with calculated total and confirm status
                order.TotalAmount = totalAmount;
                order.Status = "Confirmed";
                order.Items = orderItems;

                // Save to database
                _dbContext.Orders.Add(order);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Order {OrderId} created successfully for customer {CustomerId} with total amount {TotalAmount}", 
                    order.Id, order.CustomerId, order.TotalAmount);

                // Map to DTO for response
                var result = new OrderDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    CreatedAt = order.CreatedAt,
                    Items = orderItems.Select(item => new OrderItemDto
                    {
                        OrderId = item.OrderId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.Quantity * item.UnitPrice
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to communicate with Inventory API while creating order");
                return StatusCode(503, ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                    statusCode: 503, 
                    title: "Service unavailable", 
                    detail: "Unable to validate stock. Please try again later."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while creating order for customer {CustomerId}", dto.CustomerId);
                return StatusCode(500, ProblemDetailsFactory.CreateProblemDetails(HttpContext, 
                    statusCode: 500, 
                    title: "Internal server error", 
                    detail: "An unexpected error occurred while processing the order."));
            }
        }

        /// <summary>
        /// Gets order details by order ID.
        /// </summary>
        /// <param name="id">Order identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Order details.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var order = await _dbContext.Orders
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
        /// Gets a paginated list of orders.
        /// </summary>
        /// <param name="page">Page number (default: 1).</param>
        /// <param name="pageSize">Page size (default: 20).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of orders.</returns>
        [HttpGet]
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
                var orders = await _dbContext.Orders
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
    }
}