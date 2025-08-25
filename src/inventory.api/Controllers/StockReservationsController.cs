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
        /// Implements atomic reservation creation with comprehensive validation and error handling.
        /// </summary>
        /// <param name="request">Stock reservation request containing order details and item specifications</param>
        /// <returns>
        /// ActionResult containing StockReservationResponse with reservation details or error information
        /// </returns>
        /// <remarks>
        /// Processing Flow:
        /// 1. Validates all requested products exist and are available
        /// 2. Checks sufficient stock availability for all requested quantities
        /// 3. Creates reservations atomically within a database transaction
        /// 4. Updates available stock calculations to reflect reservations
        /// 5. Returns detailed results including reservation IDs for successful operations
        /// 
        /// Business Rules:
        /// - All-or-Nothing: Either all items are reserved successfully or no reservations are created
        /// - Atomic Operations: All reservations within a request are created in a single transaction
        /// - Stock Validation: Real-time validation against current available inventory levels
        /// - Idempotency Support: Duplicate requests for same OrderId are handled gracefully
        /// 
        /// Error Scenarios:
        /// - Product Not Found: One or more requested products don't exist or are inactive
        /// - Insufficient Stock: Requested quantities exceed available inventory levels
        /// - Duplicate Order: Reservation already exists for the specified OrderId
        /// - System Errors: Database connectivity or transaction processing failures
        /// 
        /// Performance Considerations:
        /// - Minimal locking duration through optimized query patterns
        /// - Efficient bulk operations for multi-item reservations
        /// - Database transaction scoping for consistency without excessive blocking
        /// - Comprehensive logging for monitoring and troubleshooting high-volume scenarios
        /// </remarks>
        /// <response code="201">Stock reservation successful - returns reservation details</response>
        /// <response code="400">Invalid request data - validation errors in request format</response>
        /// <response code="422">Business logic error - insufficient stock or product unavailable</response>
        /// <response code="409">Conflict - reservation already exists for the specified order</response>
        /// <response code="500">Internal server error - system failure during reservation processing</response>
        [HttpPost]
        [ProducesResponseType(typeof(StockReservationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<StockReservationResponse>> CreateReservation(
            [FromBody] StockReservationRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                _logger.LogInformation(
                    "Processing stock reservation request for Order {OrderId} with {ItemCount} items. CorrelationId: {CorrelationId}",
                    request.OrderId,
                    request.Items.Count,
                    request.CorrelationId);

                // Check if reservations already exist for this order (idempotency)
                var existingReservations = await _context.StockReservations
                    .Where(r => r.OrderId == request.OrderId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();

                if (existingReservations.Any())
                {
                    _logger.LogWarning(
                        "Stock reservation already exists for Order {OrderId}. Found {Count} existing reservations.",
                        request.OrderId,
                        existingReservations.Count);

                    return Conflict(new ProblemDetails
                    {
                        Title = "Reservation Already Exists",
                        Detail = $"Stock reservation already exists for order {request.OrderId}",
                        Status = StatusCodes.Status409Conflict
                    });
                }

                var response = new StockReservationResponse();
                var reservationsToCreate = new List<StockReservation>();

                // Validate and process each item
                foreach (var item in request.Items)
                {
                    var result = new StockReservationResult
                    {
                        ProductId = item.ProductId,
                        RequestedQuantity = item.Quantity,
                        ProductName = string.Empty // Will be populated from database
                    };

                    _logger.LogDebug(
                        "Processing reservation for Product {ProductId}, Quantity: {Quantity}",
                        item.ProductId,
                        item.Quantity);

                    // Find the product and check availability
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Product {item.ProductId} not found";
                        response.ReservationResults.Add(result);
                        
                        _logger.LogError(
                            "Product {ProductId} not found during reservation for Order {OrderId}",
                            item.ProductId,
                            request.OrderId);
                        continue;
                    }

                    result.ProductName = product.Name;
                    result.AvailableStock = product.StockQuantity;

                    // Calculate currently reserved stock for this product
                    var currentReserved = await _context.StockReservations
                        .Where(r => r.ProductId == item.ProductId && r.Status == ReservationStatus.Reserved)
                        .SumAsync(r => r.Quantity);

                    var availableForReservation = product.StockQuantity - currentReserved;

                    if (availableForReservation < item.Quantity)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Insufficient stock for product {product.Name}. Available: {availableForReservation}, Requested: {item.Quantity}";
                        response.ReservationResults.Add(result);
                        
                        _logger.LogError(
                            "Insufficient stock for Product {ProductId} ({ProductName}). Available: {Available}, Requested: {Requested}",
                            item.ProductId,
                            product.Name,
                            availableForReservation,
                            item.Quantity);
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
                        CorrelationId = request.CorrelationId
                    };

                    reservationsToCreate.Add(reservation);
                    result.Success = true;
                    result.ReservationId = reservation.Id;
                    response.ReservationResults.Add(result);

                    _logger.LogInformation(
                        "Reservation created for Product {ProductId} ({ProductName}). Quantity: {Quantity}, ReservationId: {ReservationId}",
                        item.ProductId,
                        product.Name,
                        item.Quantity,
                        reservation.Id);
                }

                // Check if all reservations were successful
                var failedReservations = response.ReservationResults.Where(r => !r.Success).ToList();
                if (failedReservations.Any())
                {
                    response.Success = false;
                    response.ErrorMessage = $"Failed to reserve stock for {failedReservations.Count} item(s). See individual results for details.";
                    
                    _logger.LogWarning(
                        "Stock reservation failed for Order {OrderId}. {FailedCount} of {TotalCount} items failed.",
                        request.OrderId,
                        failedReservations.Count,
                        request.Items.Count);
                    
                    // Don't commit transaction for failed reservations
                    return UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Stock Reservation Failed",
                        Detail = response.ErrorMessage,
                        Status = StatusCodes.Status422UnprocessableEntity
                    });
                }

                // All validations passed - create all reservations
                await _context.StockReservations.AddRangeAsync(reservationsToCreate);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Success = true;

                _logger.LogInformation(
                    "Successfully created {Count} stock reservations for Order {OrderId}",
                    reservationsToCreate.Count,
                    request.OrderId);

                return Created($"api/stockreservations/order/{request.OrderId}", response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                _logger.LogError(ex,
                    "Failed to process stock reservation for Order {OrderId}. Transaction rolled back.",
                    request.OrderId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Stock Reservation Processing Error",
                    Detail = "An error occurred while processing the stock reservation request",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
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