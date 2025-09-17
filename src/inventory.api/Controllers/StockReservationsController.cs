using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Persistence;
using InventoryApi.Domain.Entities;
using BuildingBlocks.Contracts.Inventory;
using System.Data;

namespace InventoryApi.Controllers
{
    /// <summary>
    /// API controller responsible for managing stock reservations in the inventory system.
    /// Updated to use professional domain entities instead of old models.
    /// Provides endpoints for creating, querying, and managing stock reservations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class StockReservationsController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<StockReservationsController> _logger;

        private static readonly Dictionary<Guid, SemaphoreSlim> _productLocks = new();
        private static readonly object _lockDictionaryLock = new();

        public StockReservationsController(
            InventoryDbContext context,
            ILogger<StockReservationsController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static SemaphoreSlim GetProductLock(Guid productId)
        {
            lock (_lockDictionaryLock)
            {
                if (!_productLocks.TryGetValue(productId, out var semaphore))
                {
                    semaphore = new SemaphoreSlim(1, 1);
                    _productLocks[productId] = semaphore;
                }
                return semaphore;
            }
        }

        private string GetCorrelationId()
        {
            if (HttpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId) &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId!;
            }
            return $"inv-{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Creates stock reservations for the specified products and quantities.
        /// Uses professional domain entities and business methods.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(StockReservationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<StockReservationResponse>> CreateReservation(
            [FromBody] StockReservationRequest request)
        {
            var correlationId = GetCorrelationId();
            var reservationStartTime = DateTime.UtcNow;
            var acquiredLocks = new List<SemaphoreSlim>();
            
            try
            {
                _logger.LogInformation("Starting stock reservation: Order {OrderId} | Items: {ItemCount}", 
                    request.OrderId, request.Items.Count);

                // Check for existing reservations (idempotency)
                var existingReservations = await _context.StockReservations
                    .Where(r => r.OrderId == request.OrderId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();

                if (existingReservations.Any())
                {
                    _logger.LogWarning("Stock reservation already exists for Order {OrderId}", request.OrderId);
                    return Conflict(new ProblemDetails
                    {
                        Title = "Reservation Already Exists",
                        Detail = $"Stock reservation already exists for order {request.OrderId}",
                        Status = StatusCodes.Status409Conflict
                    });
                }

                // Acquire product locks
                var productIds = request.Items.Select(i => i.ProductId).Distinct().OrderBy(id => id).ToList();
                
                foreach (var productId in productIds)
                {
                    var productLock = GetProductLock(productId);
                    await productLock.WaitAsync();
                    acquiredLocks.Add(productLock);
                }

                // Process reservations using domain methods
                var result = await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    
                    var response = new StockReservationResponse();
                    var reservationsToCreate = new List<StockReservation>();

                    // Get all required products
                    var products = await _context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .ToListAsync();

                    // Process each reservation request
                    foreach (var item in request.Items)
                    {
                        var stockResult = new StockReservationResult
                        {
                            ProductId = item.ProductId,
                            RequestedQuantity = item.Quantity,
                            ProductName = string.Empty
                        };

                        var product = products.FirstOrDefault(p => p.Id == item.ProductId);

                        if (product == null)
                        {
                            stockResult.Success = false;
                            stockResult.ErrorMessage = $"Product {item.ProductId} not found";
                            response.ReservationResults.Add(stockResult);
                            continue;
                        }

                        stockResult.ProductName = product.Name;
                        stockResult.AvailableStock = product.StockQuantity;

                        // Use domain method to check stock availability
                        if (!product.HasSufficientStock(item.Quantity))
                        {
                            stockResult.Success = false;
                            stockResult.ErrorMessage = $"Insufficient stock for product {product.Name}. Available: {product.StockQuantity}, Requested: {item.Quantity}";
                            response.ReservationResults.Add(stockResult);
                            continue;
                        }

                        try
                        {
                            // Use domain method to reserve stock
                            product.ReserveStock(item.Quantity, "api-reservation");

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

                            _logger.LogInformation("Stock reserved for Product {ProductId}, Quantity: {Quantity}", 
                                item.ProductId, item.Quantity);
                        }
                        catch (Exception ex)
                        {
                            stockResult.Success = false;
                            stockResult.ErrorMessage = ex.Message;
                            response.ReservationResults.Add(stockResult);
                            
                            _logger.LogError(ex, "Failed to reserve stock for Product {ProductId}", item.ProductId);
                        }
                    }

                    // Check if all reservations succeeded
                    var failedReservations = response.ReservationResults.Where(r => !r.Success).ToList();
                    if (failedReservations.Any())
                    {
                        response.Success = false;
                        response.ErrorMessage = $"Failed to reserve stock for {failedReservations.Count} item(s)";
                        await transaction.RollbackAsync();
                        return response;
                    }

                    // Save all reservations
                    await _context.StockReservations.AddRangeAsync(reservationsToCreate);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Success = true;

                    _logger.LogInformation("Stock reservations created for Order {OrderId}, Count: {Count}", 
                        request.OrderId, reservationsToCreate.Count);

                    return response;
                });

                if (!result.Success)
                {
                    return UnprocessableEntity(new ProblemDetails
                    {
                        Title = "Stock Reservation Failed",
                        Detail = result.ErrorMessage,
                        Status = StatusCodes.Status422UnprocessableEntity
                    });
                }

                return Created($"api/stockreservations/order/{request.OrderId}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock reservation failed for Order {OrderId}", request.OrderId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Stock Reservation Processing Error",
                    Detail = "An error occurred while processing the stock reservation request",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
            finally
            {
                // Release locks in reverse order
                acquiredLocks.Reverse();
                foreach (var lockItem in acquiredLocks)
                {
                    lockItem.Release();
                }
            }
        }

        /// <summary>
        /// Retrieves stock reservations for a specific order.
        /// </summary>
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
                return NotFound(new ProblemDetails
                {
                    Title = "Reservations Not Found",
                    Detail = $"No stock reservations found for order {orderId}",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(reservations);
        }

        /// <summary>
        /// Retrieves a specific stock reservation by its unique identifier.
        /// </summary>
        [HttpGet("{reservationId:guid}")]
        [ProducesResponseType(typeof(StockReservation), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StockReservation>> GetReservation(Guid reservationId)
        {
            var reservation = await _context.StockReservations
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Reservation Not Found",
                    Detail = $"Stock reservation {reservationId} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(reservation);
        }
    }
}