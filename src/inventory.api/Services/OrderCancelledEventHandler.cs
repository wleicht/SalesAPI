using BuildingBlocks.Events.Domain;
using InventoryApi.Persistence;
using InventoryApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Rebus.Handlers;
using Rebus.Bus;

namespace InventoryApi.Services
{
    /// <summary>
    /// Event handler responsible for processing OrderCancelledEvent messages from the Sales API.
    /// Updated to use professional domain entities instead of old models.
    /// Implements automatic stock reservation release with full transactional support and idempotency guarantees.
    /// </summary>
    public class OrderCancelledEventHandler : IHandleMessages<OrderCancelledEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly IBus _bus;
        private readonly ILogger<OrderCancelledEventHandler> _logger;

        public OrderCancelledEventHandler(
            InventoryDbContext dbContext,
            IBus bus,
            ILogger<OrderCancelledEventHandler> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes an OrderCancelledEvent by releasing associated stock reservations.
        /// Uses professional domain entities and their business methods.
        /// </summary>
        public async Task Handle(OrderCancelledEvent cancelEvent)
        {
            _logger.LogInformation("Processing OrderCancelledEvent for Order {OrderId}, Reason: {Reason}", 
                cancelEvent.OrderId, cancelEvent.CancellationReason);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Check for idempotency
                var existingProcessedEvent = await _dbContext.ProcessedEvents
                    .FirstOrDefaultAsync(pe => pe.EventId == cancelEvent.EventId);

                if (existingProcessedEvent != null)
                {
                    _logger.LogWarning("Event {EventId} for Order {OrderId} already processed", 
                        cancelEvent.EventId, cancelEvent.OrderId);
                    return;
                }

                // Find active reservations that can be released
                var activeReservations = await _dbContext.StockReservations
                    .Where(r => r.OrderId == cancelEvent.OrderId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();

                if (!activeReservations.Any())
                {
                    _logger.LogInformation("No active reservations found for Order {OrderId}", cancelEvent.OrderId);
                    
                    // Mark event as processed
                    var processedEvent = new ProcessedEvent
                    {
                        EventId = cancelEvent.EventId,
                        EventType = nameof(OrderCancelledEvent),
                        OrderId = cancelEvent.OrderId,
                        CorrelationId = cancelEvent.CorrelationId,
                        ProcessingDetails = "No active reservations found to release"
                    };

                    _dbContext.ProcessedEvents.Add(processedEvent);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return;
                }

                _logger.LogInformation("Found {ReservationCount} active reservations for Order {OrderId}", 
                    activeReservations.Count, cancelEvent.OrderId);

                var releasedReservations = new List<StockReservation>();
                int totalQuantityReleased = 0;

                // Process each active reservation using domain methods
                foreach (var reservation in activeReservations)
                {
                    try
                    {
                        // Find the product to release stock back
                        var product = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.Id == reservation.ProductId);

                        if (product != null)
                        {
                            // Use domain method to release reserved stock
                            product.ReleaseReservedStock(reservation.Quantity, "order-cancelled-handler");
                            
                            _logger.LogInformation("Released {Quantity} units back to Product {ProductId} stock", 
                                reservation.Quantity, reservation.ProductId);
                        }
                        else
                        {
                            _logger.LogWarning("Product {ProductId} not found when releasing reservation", 
                                reservation.ProductId);
                        }

                        // Update reservation status to Released
                        reservation.Status = ReservationStatus.Released;
                        reservation.ProcessedAt = DateTime.UtcNow;

                        releasedReservations.Add(reservation);
                        totalQuantityReleased += reservation.Quantity;

                        _logger.LogInformation("Stock reservation {ReservationId} released for Product {ProductId}", 
                            reservation.Id, reservation.ProductId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error releasing reservation {ReservationId} for Product {ProductId}", 
                            reservation.Id, reservation.ProductId);
                        // Continue with other reservations
                    }
                }

                // Mark event as processed
                var eventProcessedRecord = new ProcessedEvent
                {
                    EventId = cancelEvent.EventId,
                    EventType = nameof(OrderCancelledEvent),
                    OrderId = cancelEvent.OrderId,
                    CorrelationId = cancelEvent.CorrelationId,
                    ProcessingDetails = $"Released {releasedReservations.Count} reservations, total quantity: {totalQuantityReleased}. Reason: {cancelEvent.CancellationReason}"
                };

                _dbContext.ProcessedEvents.Add(eventProcessedRecord);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully released {Count} reservations for Order {OrderId}", 
                    releasedReservations.Count, cancelEvent.OrderId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to process OrderCancelledEvent for Order {OrderId}", cancelEvent.OrderId);
                throw;
            }
        }
    }
}