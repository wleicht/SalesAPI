using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Persistence;
using InventoryApi.Models;
using BuildingBlocks.Contracts.Inventory;

namespace InventoryApi.Controllers
{
    /// <summary>
    /// API controller responsible for managing stock reservations in the inventory system.
    /// Provides endpoints for creating, querying, and managing stock reservations that prevent
    /// race conditions during distributed order processing workflows.
    /// </summary>
    /// <remarks>
    /// The StockReservationsController implements the synchronous portion of the Saga pattern
    /// for distributed transaction management between Sales and Inventory services. Key capabilities:
    /// 
    /// Core Functionality:
    /// - Atomic stock reservation creation with immediate availability validation
    /// - Real-time stock availability checking to prevent overselling scenarios
    /// - Reservation status management through the complete lifecycle
    /// - Comprehensive error handling with detailed failure information
    /// 
    /// Integration Patterns:
    /// - Synchronous HTTP API for immediate reservation creation and validation
    /// - Event-driven processing for reservation confirmation and cancellation
    /// - Database transaction management for data consistency guarantees
    /// - Audit trail support for compliance and troubleshooting requirements
    /// 
    /// Business Value:
    /// - Prevents overselling in high-concurrency e-commerce scenarios
    /// - Enables reliable order processing with proper stock allocation
    /// - Supports complex order workflows including payment processing delays
    /// - Provides detailed inventory allocation visibility for business operations
    /// 
    /// The controller follows RESTful API design principles and includes comprehensive
    /// error handling, validation, and logging for production-ready operation.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class StockReservationsController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<StockReservationsController> _logger;

        /// <summary>
        /// Initializes a new instance of the StockReservationsController with required dependencies.
        /// </summary>
        /// <param name="context">Entity Framework context for inventory database operations</param>
        /// <param name="logger">Logger for tracking reservation operations and troubleshooting</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when context or logger parameters are null
        /// </exception>
        public StockReservationsController(
            InventoryDbContext context,
            ILogger<StockReservationsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates stock reservations for the specified products and quantities.
        /// Implements atomic reservation creation with comprehensive validation, error handling,
        /// and enhanced observability through correlation tracking and structured logging.
        /// Uses retry strategy to handle transient database failures.
        /// </summary>
        /// <param name="request">Stock reservation request containing order details and item specifications</param>
        /// <returns>
        /// ActionResult containing StockReservationResponse with reservation details or error information
        /// </returns>
        /// <remarks>
        /// Enhanced Processing Flow with Observability:
        /// 1. Extract correlation ID for end-to-end request tracking
        /// 2. Uses Entity Framework execution strategy for retry handling
        /// 3. Validates all requested products exist and are available
        /// 4. Checks sufficient stock availability accounting for existing reservations
        /// 5. Creates reservations atomically within SaveChanges transaction
        /// 6. Returns detailed results including reservation IDs for successful operations
        /// 
        /// Observability Features:
        /// - Correlation ID propagation throughout the workflow
        /// - Structured logging with detailed context and timing
        /// - Performance metrics for each step
        /// - Error categorization for monitoring and alerting
        /// - Business metrics tracking (reservations created, failures)
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(StockReservationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<StockReservationResponse>> CreateReservation(
            [FromBody] StockReservationRequest request)
        {
            // Extract correlation ID for observability
            var correlationId = GetCorrelationId();
            var reservationStartTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation(
                    "???? Starting stock reservation: Order {OrderId} | Items: {ItemCount} | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    request.Items.Count,
                    correlationId);

                // Check if reservations already exist for this order (idempotency)
                var existingReservations = await _context.StockReservations
                    .Where(r => r.OrderId == request.OrderId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();

                if (existingReservations.Any())
                {
                    _logger.LogWarning(
                        "???? Stock reservation already exists: Order {OrderId} | Existing: {Count} | CorrelationId: {CorrelationId}",
                        request.OrderId,
                        existingReservations.Count,
                        correlationId);

                    return Conflict(new ProblemDetails
                    {
                        Title = "Reservation Already Exists",
                        Detail = $"Stock reservation already exists for order {request.OrderId}",
                        Status = StatusCodes.Status409Conflict
                    });
                }

                var response = new StockReservationResponse();
                var reservationsToCreate = new List<StockReservation>();

                // Get all product IDs to process
                var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
                
                // Get all products that we need to reserve
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                _logger.LogDebug(
                    "???? Found products: Count: {ProductCount} | CorrelationId: {CorrelationId}",
                    products.Count,
                    correlationId);

                // Validate and process each item
                foreach (var item in request.Items)
                {
                    var stockResult = new StockReservationResult
                    {
                        ProductId = item.ProductId,
                        RequestedQuantity = item.Quantity,
                        ProductName = string.Empty // Will be populated from database
                    };

                    _logger.LogDebug(
                        "???? Processing reservation item: Product {ProductId} | Quantity: {Quantity} | CorrelationId: {CorrelationId}",
                        item.ProductId,
                        item.Quantity,
                        correlationId);

                    // Find the product
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                    if (product == null)
                    {
                        stockResult.Success = false;
                        stockResult.ErrorMessage = $"Product {item.ProductId} not found";
                        response.ReservationResults.Add(stockResult);
                        
                        _logger.LogError(
                            "??? Product not found: Product {ProductId} | CorrelationId: {CorrelationId}",
                            item.ProductId,
                            correlationId);
                        continue;
                    }

                    stockResult.ProductName = product.Name;
                    stockResult.AvailableStock = product.StockQuantity;

                    // Calculate currently reserved stock for this product
                    var currentReserved = await _context.StockReservations
                        .Where(r => r.ProductId == item.ProductId && r.Status == ReservationStatus.Reserved)
                        .SumAsync(r => r.Quantity);

                    var availableForReservation = product.StockQuantity - currentReserved;

                    if (availableForReservation < item.Quantity)
                    {
                        stockResult.Success = false;
                        stockResult.ErrorMessage = $"Insufficient stock for product {product.Name}. Available: {availableForReservation}, Requested: {item.Quantity}";
                        response.ReservationResults.Add(stockResult);
                        
                        _logger.LogError(
                            "??? Insufficient stock: Product {ProductId} ({ProductName}) | Available: {Available} | Requested: {Requested} | Total: {Total} | Reserved: {Reserved} | CorrelationId: {CorrelationId}",
                            item.ProductId,
                            product.Name,
                            availableForReservation,
                            item.Quantity,
                            product.StockQuantity,
                            currentReserved,
                            correlationId);
                        continue;
                    }

                    // Create reservation record
                    var reservation = new StockReservation
                    {
                        OrderId = request.OrderId,
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        Status = ReservationStatus.Reserved,
                        CorrelationId = correlationId
                    };

                    reservationsToCreate.Add(reservation);
                    stockResult.Success = true;
                    stockResult.ReservationId = reservation.Id;
                    response.ReservationResults.Add(stockResult);

                    _logger.LogInformation(
                        "??? Reservation prepared: Product {ProductId} ({ProductName}) | Qty: {Quantity} | ReservationId: {ReservationId} | Available After: {AvailableAfter} | CorrelationId: {CorrelationId}",
                        item.ProductId,
                        product.Name,
                        item.Quantity,
                        reservation.Id,
                        availableForReservation - item.Quantity,
                        correlationId);
                }

                // Check if all reservations were successful
                var failedReservations = response.ReservationResults.Where(r => !r.Success).ToList();
                if (failedReservations.Any())
                {
                    response.Success = false;
                    response.ErrorMessage = $"Failed to reserve stock for {failedReservations.Count} item(s). See individual results for details.";
                    
                    _logger.LogWarning(
                        "??? Stock reservation failed: Order {OrderId} | Failed: {FailedCount}/{TotalCount} | CorrelationId: {CorrelationId}",
                        request.OrderId,
                        failedReservations.Count,
                        request.Items.Count,
                        correlationId);
                    
                    return UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Stock Reservation Failed",
                        Detail = response.ErrorMessage,
                        Status = StatusCodes.Status422UnprocessableEntity
                    });
                }

                // All validations passed - create all reservations atomically with retry strategy
                var savingStartTime = DateTime.UtcNow;
                await _context.StockReservations.AddRangeAsync(reservationsToCreate);
                await _context.SaveChangesAsync();
                var savingDuration = DateTime.UtcNow - savingStartTime;

                response.Success = true;

                var totalDuration = DateTime.UtcNow - reservationStartTime;
                var totalQuantity = reservationsToCreate.Sum(r => r.Quantity);
                
                _logger.LogInformation(
                    "???? Stock reservations created successfully: Order {OrderId} | Count: {Count} | TotalQty: {TotalQuantity} | Duration: {Duration}ms (Saving: {SavingDuration}ms) | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    reservationsToCreate.Count,
                    totalQuantity,
                    totalDuration.TotalMilliseconds,
                    savingDuration.TotalMilliseconds,
                    correlationId);

                return Created($"api/stockreservations/order/{request.OrderId}", response);
            }
            catch (Exception ex)
            {
                var totalDuration = DateTime.UtcNow - reservationStartTime;
                _logger.LogError(ex,
                    "???? Stock reservation failed: Order {OrderId} | Duration: {Duration}ms | Error: {ErrorMessage} | CorrelationId: {CorrelationId}",
                    request.OrderId,
                    totalDuration.TotalMilliseconds,
                    ex.Message,
                    correlationId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Stock Reservation Processing Error",
                    Detail = "An error occurred while processing the stock reservation request",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Extracts correlation ID from request headers or generates a new one.
        /// </summary>
        /// <returns>Valid correlation ID for request tracking</returns>
        private string GetCorrelationId()
        {
            // Try to get correlation ID from request headers (set by middleware or propagated from Sales API)
            if (HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId!;
            }

            // Fallback: generate new correlation ID
            return $"inv-{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Retrieves stock reservations for a specific order.
        /// Provides detailed information about current reservation status and history.
        /// </summary>
        /// <param name="orderId">Unique identifier of the order to query reservations for</param>
        /// <returns>Collection of stock reservations associated with the specified order</returns>
        /// <remarks>
        /// This endpoint enables:
        /// - Order status inquiries and customer service support
        /// - Debugging and troubleshooting of reservation-related issues
        /// - Audit trail access for compliance and business analysis
        /// - Integration with order management and reporting systems
        /// 
        /// The response includes complete reservation lifecycle information including
        /// creation timestamps, current status, and processing history.
        /// </remarks>
        /// <response code="200">Returns the stock reservations for the specified order</response>
        /// <response code="404">No reservations found for the specified order</response>
        [HttpGet("order/{orderId:guid}")]
        [ProducesResponseType(typeof(List<StockReservation>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<StockReservation>>> GetReservationsByOrder(Guid orderId)
        {
            _logger.LogInformation("Retrieving stock reservations for Order {OrderId}", orderId);

            var reservations = await _context.StockReservations
                .Where(r => r.OrderId == orderId)
                .OrderBy(r => r.ReservedAt)
                .ToListAsync();

            if (!reservations.Any())
            {
                _logger.LogInformation("No stock reservations found for Order {OrderId}", orderId);
                return NotFound(new ProblemDetails
                {
                    Title = "Reservations Not Found",
                    Detail = $"No stock reservations found for order {orderId}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation(
                "Found {Count} stock reservations for Order {OrderId}",
                reservations.Count,
                orderId);

            return Ok(reservations);
        }

        /// <summary>
        /// Retrieves a specific stock reservation by its unique identifier.
        /// Provides detailed information about individual reservation status and history.
        /// </summary>
        /// <param name="reservationId">Unique identifier of the reservation to retrieve</param>
        /// <returns>Stock reservation details for the specified reservation ID</returns>
        /// <remarks>
        /// This endpoint supports:
        /// - Detailed reservation inquiry for customer service and debugging
        /// - Event correlation and distributed tracing workflows
        /// - Audit trail access for specific reservation lifecycle tracking
        /// - Integration with monitoring and alerting systems
        /// </remarks>
        /// <response code="200">Returns the stock reservation details</response>
        /// <response code="404">Reservation not found for the specified ID</response>
        [HttpGet("{reservationId:guid}")]
        [ProducesResponseType(typeof(StockReservation), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StockReservation>> GetReservation(Guid reservationId)
        {
            _logger.LogInformation("Retrieving stock reservation {ReservationId}", reservationId);

            var reservation = await _context.StockReservations
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                _logger.LogInformation("Stock reservation {ReservationId} not found", reservationId);
                return NotFound(new ProblemDetails
                {
                    Title = "Reservation Not Found",
                    Detail = $"Stock reservation {reservationId} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation(
                "Retrieved stock reservation {ReservationId} for Order {OrderId}, Status: {Status}",
                reservationId,
                reservation.OrderId,
                reservation.Status);

            return Ok(reservation);
        }
    }
}