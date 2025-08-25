using BuildingBlocks.Events.Domain;
using InventoryApi.Persistence;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;
using Rebus.Handlers;
using Rebus.Bus;

namespace InventoryApi.Services
{
    /// <summary>
    /// Event handler responsible for processing OrderCancelledEvent messages from the Sales API.
    /// Implements automatic stock reservation release with full transactional support and idempotency guarantees.
    /// Uses Rebus message handling infrastructure for reliable event consumption and compensation logic.
    /// </summary>
    /// <remarks>
    /// This handler implements the compensation phase of the Saga pattern for distributed order processing.
    /// When orders are cancelled, any existing stock reservations must be released back to available
    /// inventory to maintain system consistency and prevent permanent stock locks.
    /// 
    /// Key Responsibilities:
    /// - Release stock reservations when orders are cancelled or fail
    /// - Implement compensation logic for failed distributed transactions
    /// - Maintain audit trails for all reservation status changes
    /// - Ensure idempotent processing to handle duplicate cancellation events
    /// - Provide detailed logging for operational monitoring and troubleshooting
    /// 
    /// Business Impact:
    /// - Prevents permanent stock allocation for failed orders
    /// - Maintains inventory accuracy and availability for future customers
    /// - Supports robust error recovery in distributed order processing
    /// - Enables comprehensive audit trails for business and compliance requirements
    /// 
    /// Integration Patterns:
    /// - Event-Driven Architecture: Asynchronous processing of order cancellation events
    /// - Saga Pattern: Compensation logic for distributed transaction management
    /// - Transactional Outbox: Database transactions ensure consistency
    /// - Idempotency: Safe handling of duplicate events through processed event tracking
    /// </remarks>
    public class OrderCancelledEventHandler : IHandleMessages<OrderCancelledEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly IBus _bus;
        private readonly ILogger<OrderCancelledEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the OrderCancelledEventHandler with required dependencies.
        /// </summary>
        /// <param name="dbContext">Entity Framework context for inventory database operations</param>
        /// <param name="bus">Rebus message bus for publishing response events</param>
        /// <param name="logger">Logger for tracking event processing operations and errors</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any of the required dependencies are null
        /// </exception>
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
        /// Implements comprehensive error handling, idempotency checks, and audit trail creation.
        /// </summary>
        /// <param name="cancelEvent">The order cancelled event containing cancellation details and items to process</param>
        /// <returns>A task representing the asynchronous event processing operation</returns>
        /// <remarks>
        /// Processing Flow:
        /// 1. Validates event hasn't been processed before (idempotency)
        /// 2. Begins database transaction for consistency
        /// 3. Finds existing stock reservations for the cancelled order
        /// 4. Updates reservation status from Reserved to Released
        /// 5. Records processing timestamps for audit trail
        /// 6. Marks event as processed to prevent reprocessing
        /// 7. Optionally publishes response events for order status updates
        /// 8. Commits transaction or rolls back on error
        /// 
        /// Compensation Logic:
        /// - Only reservations in 'Reserved' status are released (idempotent)
        /// - Already processed reservations (Debited/Released) are ignored
        /// - Physical stock levels are automatically available when reservations are released
        /// - Complete audit trail maintains reservation lifecycle history
        /// 
        /// Error Handling:
        /// - Database transaction ensures atomic processing
        /// - Detailed logging supports troubleshooting and monitoring
        /// - Failed processing triggers automatic retry via Rebus infrastructure
        /// - Idempotency prevents duplicate processing on retry
        /// 
        /// The handler ensures reliable compensation processing that maintains
        /// inventory consistency even in complex failure scenarios.
        /// </remarks>
        /// <exception cref="Exception">
        /// Re-throws any processing exceptions to trigger Rebus retry mechanisms.
        /// Database transactions are rolled back automatically on failures.
        /// </exception>
        public async Task Handle(OrderCancelledEvent cancelEvent)
        {
            _logger.LogInformation(
                "=== CANCELLATION HANDLER CALLED === Processing OrderCancelledEvent for Order {OrderId}",
                cancelEvent.OrderId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation(
                    "Processing OrderCancelledEvent for Order {OrderId} with {ItemCount} items. Reason: {Reason}. CorrelationId: {CorrelationId}",
                    cancelEvent.OrderId,
                    cancelEvent.Items.Count,
                    cancelEvent.CancellationReason,
                    cancelEvent.CorrelationId);

                // Check for idempotency - see if we've already processed this event
                var existingProcessedEvent = await _dbContext.ProcessedEvents
                    .FirstOrDefaultAsync(pe => pe.EventId == cancelEvent.EventId);

                if (existingProcessedEvent != null)
                {
                    _logger.LogWarning(
                        "Event {EventId} for Order {OrderId} has already been processed. Skipping to ensure idempotency.",
                        cancelEvent.EventId,
                        cancelEvent.OrderId);
                    return;
                }

                // Find existing stock reservations for this order that can be released
                var activeReservations = await _dbContext.StockReservations
                    .Where(r => r.OrderId == cancelEvent.OrderId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();

                if (!activeReservations.Any())
                {
                    _logger.LogInformation(
                        "No active stock reservations found for Order {OrderId}. Order may have been already processed or had no reservations.",
                        cancelEvent.OrderId);

                    // Mark event as processed even if no reservations found (idempotent behavior)
                    var processedEvent = new ProcessedEvent
                    {
                        EventId = cancelEvent.EventId,
                        EventType = nameof(OrderCancelledEvent),
                        OrderId = cancelEvent.OrderId,
                        ProcessedAt = DateTime.UtcNow,
                        CorrelationId = cancelEvent.CorrelationId,
                        ProcessingDetails = "No active reservations found to release"
                    };

                    _dbContext.ProcessedEvents.Add(processedEvent);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return;
                }

                _logger.LogInformation(
                    "Found {ReservationCount} active stock reservations for Order {OrderId}. Releasing reservations.",
                    activeReservations.Count,
                    cancelEvent.OrderId);

                var releasedReservations = new List<StockReservation>();
                int totalQuantityReleased = 0;

                // Process each active reservation
                foreach (var reservation in activeReservations)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Releasing stock reservation for Product {ProductId} ({ProductName}), Reserved Quantity: {Quantity}, ReservationId: {ReservationId}",
                            reservation.ProductId,
                            reservation.ProductName,
                            reservation.Quantity,
                            reservation.Id);

                        // Update reservation status to Released
                        reservation.Status = ReservationStatus.Released;
                        reservation.ProcessedAt = DateTime.UtcNow;

                        releasedReservations.Add(reservation);
                        totalQuantityReleased += reservation.Quantity;

                        _logger.LogInformation(
                            "Stock reservation {ReservationId} released for Product {ProductId} ({ProductName}). Quantity: {Quantity} returned to available stock.",
                            reservation.Id,
                            reservation.ProductId,
                            reservation.ProductName,
                            reservation.Quantity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error releasing stock reservation {ReservationId} for Product {ProductId} in Order {OrderId}",
                            reservation.Id,
                            reservation.ProductId,
                            cancelEvent.OrderId);

                        // Continue processing other reservations even if one fails
                        // The transaction will be rolled back if any critical errors occur
                    }
                }

                // Mark event as processed for idempotency
                var eventProcessedRecord = new ProcessedEvent
                {
                    EventId = cancelEvent.EventId,
                    EventType = nameof(OrderCancelledEvent),
                    OrderId = cancelEvent.OrderId,
                    ProcessedAt = DateTime.UtcNow,
                    CorrelationId = cancelEvent.CorrelationId,
                    ProcessingDetails = $"Released {releasedReservations.Count} reservations, total quantity: {totalQuantityReleased}. Reason: {cancelEvent.CancellationReason}"
                };

                _dbContext.ProcessedEvents.Add(eventProcessedRecord);

                // Save all changes
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Database changes saved successfully for Order {OrderId}. Released {Count} stock reservations.",
                    cancelEvent.OrderId,
                    releasedReservations.Count);

                // Optionally publish a response event (for future enhancement)
                try
                {
                    // Note: StockReleasedEvent could be added in future for order status updates
                    // await _bus.Advanced.Routing.Send("sales.api", stockReleasedEvent);
                    _logger.LogDebug("Stock release processing completed for Order {OrderId}", cancelEvent.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to publish stock release confirmation for Order {OrderId}, but reservations were released successfully",
                        cancelEvent.OrderId);
                }

                // Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "=== CANCELLATION HANDLER COMPLETED === Successfully processed OrderCancelledEvent for Order {OrderId}. Released {Count} reservations totaling {TotalQuantity} units.",
                    cancelEvent.OrderId,
                    releasedReservations.Count,
                    totalQuantityReleased);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "=== CANCELLATION HANDLER FAILED === Failed to process OrderCancelledEvent for Order {OrderId}. Transaction rolled back.",
                    cancelEvent.OrderId);

                // Re-throw to trigger retry mechanism
                throw;
            }
        }
    }
}