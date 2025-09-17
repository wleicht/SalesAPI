using BuildingBlocks.Events.Domain;
using InventoryApi.Persistence;
using InventoryApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Rebus.Handlers;
using Rebus.Bus;

namespace InventoryApi.Services
{
    /// <summary>
    /// Event handler responsible for processing OrderConfirmedEvent messages from the Sales API.
    /// Updated to use professional domain entities instead of old models.
    /// Implements automatic stock deduction with full transactional support and idempotency guarantees.
    /// </summary>
    public class OrderConfirmedEventHandler : IHandleMessages<OrderConfirmedEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly IBus _bus;
        private readonly ILogger<OrderConfirmedEventHandler> _logger;

        public OrderConfirmedEventHandler(
            InventoryDbContext dbContext,
            IBus bus,
            ILogger<OrderConfirmedEventHandler> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes an OrderConfirmedEvent by converting stock reservations to debited status.
        /// Uses professional domain entities and their business methods.
        /// </summary>
        public async Task Handle(OrderConfirmedEvent orderEvent)
        {
            _logger.LogInformation("Processing OrderConfirmedEvent for Order {OrderId} with {ItemCount} items", 
                orderEvent.OrderId, orderEvent.Items.Count);

            try
            {
                // Check for idempotency
                var existingProcessedEvent = await _dbContext.ProcessedEvents
                    .FirstOrDefaultAsync(pe => pe.EventId == orderEvent.EventId);

                if (existingProcessedEvent != null)
                {
                    _logger.LogWarning("Event {EventId} for Order {OrderId} already processed", 
                        orderEvent.EventId, orderEvent.OrderId);
                    return;
                }

                // Find existing stock reservations for this order
                var stockReservations = await _dbContext.StockReservations
                    .Where(r => r.OrderId == orderEvent.OrderId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();

                if (!stockReservations.Any())
                {
                    _logger.LogError("No active stock reservations found for Order {OrderId}", orderEvent.OrderId);
                    
                    // Mark event as processed with error
                    var errorProcessedEvent = new ProcessedEvent
                    {
                        EventId = orderEvent.EventId,
                        EventType = nameof(OrderConfirmedEvent),
                        OrderId = orderEvent.OrderId,
                        CorrelationId = orderEvent.CorrelationId,
                        ProcessingDetails = "No active stock reservations found for order"
                    };

                    _dbContext.ProcessedEvents.Add(errorProcessedEvent);
                    await _dbContext.SaveChangesAsync();
                    return;
                }

                var stockDeductions = new List<StockDeduction>();
                bool allDeductionsSuccessful = true;
                string? errorMessage = null;

                _logger.LogInformation("Found {ReservationCount} stock reservations for Order {OrderId}", 
                    stockReservations.Count, orderEvent.OrderId);

                // Process each reservation using domain methods
                foreach (var reservation in stockReservations)
                {
                    try
                    {
                        var product = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.Id == reservation.ProductId);

                        if (product == null)
                        {
                            _logger.LogError("Product {ProductId} not found for Order {OrderId}", 
                                reservation.ProductId, orderEvent.OrderId);
                            allDeductionsSuccessful = false;
                            errorMessage = $"Product {reservation.ProductId} not found";
                            continue;
                        }

                        // Use domain method to allocate stock (final commitment)
                        if (!product.HasSufficientStock(reservation.Quantity))
                        {
                            _logger.LogError("Insufficient stock for Product {ProductId} in Order {OrderId}", 
                                reservation.ProductId, orderEvent.OrderId);
                            allDeductionsSuccessful = false;
                            errorMessage = $"Insufficient stock for product {reservation.ProductName}";
                            continue;
                        }

                        var previousStock = product.StockQuantity;
                        
                        // Use domain method for stock allocation
                        product.AllocateStock(reservation.Quantity, "order-confirmed-handler");

                        // Update reservation status to debited
                        reservation.Status = ReservationStatus.Debited;
                        reservation.ProcessedAt = DateTime.UtcNow;

                        var stockDeduction = new StockDeduction
                        {
                            ProductId = reservation.ProductId,
                            ProductName = reservation.ProductName,
                            QuantityDebited = reservation.Quantity,
                            PreviousStock = previousStock,
                            NewStock = product.StockQuantity
                        };

                        stockDeductions.Add(stockDeduction);

                        _logger.LogInformation("Stock allocated for Product {ProductId}. Previous: {Previous}, Allocated: {Allocated}, New: {New}", 
                            reservation.ProductId, previousStock, reservation.Quantity, product.StockQuantity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing stock allocation for Product {ProductId} in Order {OrderId}", 
                            reservation.ProductId, orderEvent.OrderId);
                        allDeductionsSuccessful = false;
                        errorMessage = $"Error processing product {reservation.ProductName}: {ex.Message}";
                    }
                }

                // Mark event as processed
                var processedEvent = new ProcessedEvent
                {
                    EventId = orderEvent.EventId,
                    EventType = nameof(OrderConfirmedEvent),
                    OrderId = orderEvent.OrderId,
                    CorrelationId = orderEvent.CorrelationId,
                    ProcessingDetails = allDeductionsSuccessful 
                        ? $"Successfully processed {stockDeductions.Count} stock allocations"
                        : $"Partial processing: {stockDeductions.Count} successful, errors: {errorMessage}"
                };

                _dbContext.ProcessedEvents.Add(processedEvent);
                await _dbContext.SaveChangesAsync();

                // Publish response event
                try
                {
                    var stockDebitedEvent = new StockDebitedEvent
                    {
                        OrderId = orderEvent.OrderId,
                        StockDeductions = stockDeductions,
                        AllDeductionsSuccessful = allDeductionsSuccessful,
                        ErrorMessage = errorMessage,
                        CorrelationId = orderEvent.CorrelationId
                    };

                    await _bus.Advanced.Routing.Send("sales.api", stockDebitedEvent);
                    _logger.LogInformation("Published StockDebitedEvent for Order {OrderId}", orderEvent.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish StockDebitedEvent for Order {OrderId}", orderEvent.OrderId);
                }

                _logger.LogInformation("Successfully processed OrderConfirmedEvent for Order {OrderId}. Success: {Success}", 
                    orderEvent.OrderId, allDeductionsSuccessful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process OrderConfirmedEvent for Order {OrderId}", orderEvent.OrderId);
                throw;
            }
        }
    }
}