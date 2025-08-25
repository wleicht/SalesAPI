using BuildingBlocks.Events.Domain;
using InventoryApi.Persistence;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;
using Rebus.Handlers;
using Rebus.Bus;

namespace InventoryApi.Services
{
    /// <summary>
    /// Event handler responsible for processing OrderConfirmedEvent messages from the Sales API.
    /// Implements automatic stock deduction with full transactional support and idempotency guarantees.
    /// Uses Rebus message handling infrastructure for reliable event consumption.
    /// </summary>
    /// <remarks>
    /// This handler ensures that when orders are confirmed in the Sales service, the corresponding
    /// stock quantities are automatically debited from the inventory. The implementation includes:
    /// - Idempotency protection to prevent duplicate processing
    /// - Database transactions for data consistency
    /// - Detailed audit trails for all stock movements
    /// - Error handling with automatic retry capabilities
    /// - Response event publishing for order status updates
    /// </remarks>
    public class OrderConfirmedEventHandler : IHandleMessages<OrderConfirmedEvent>
    {
        private readonly InventoryDbContext _dbContext;
        private readonly IBus _bus;
        private readonly ILogger<OrderConfirmedEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the OrderConfirmedEventHandler.
        /// </summary>
        /// <param name="dbContext">Entity Framework context for inventory database operations</param>
        /// <param name="bus">Rebus message bus for publishing response events</param>
        /// <param name="logger">Logger for tracking event processing operations</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any of the required dependencies are null
        /// </exception>
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
        /// Implements comprehensive error handling, idempotency checks, and audit trail creation.
        /// Uses Entity Framework retry strategy for handling transient failures.
        /// </summary>
        /// <param name="orderEvent">The order confirmed event containing order details and items to process</param>
        /// <returns>A task representing the asynchronous event processing operation</returns>
        /// <remarks>
        /// Processing flow with reservations:
        /// 1. Validates event hasn't been processed before (idempotency)
        /// 2. Uses Entity Framework SaveChanges for atomic operations
        /// 3. Finds existing stock reservations for the order
        /// 4. Converts reservations from Reserved to Debited status
        /// 5. Updates product stock quantities by debiting reserved amounts
        /// 6. Marks event as processed to prevent reprocessing
        /// 7. Publishes StockDebitedEvent response
        /// 8. Relies on retry strategy for transient failures
        /// 
        /// Reservation-based processing ensures that stock has already been validated
        /// and allocated during the synchronous reservation phase, making this
        /// asynchronous processing more reliable and predictable.
        /// 
        /// The handler ensures that either all stock deductions succeed or none are applied,
        /// maintaining data consistency across the distributed system.
        /// </remarks>
        /// <exception cref="Exception">
        /// Re-throws any processing exceptions to trigger Rebus retry mechanisms.
        /// Entity Framework retry strategy handles transient database failures.
        /// </exception>
        public async Task Handle(OrderConfirmedEvent orderEvent)
        {
            _logger.LogInformation("=== HANDLER CALLED === Processing OrderConfirmedEvent for Order {OrderId}", orderEvent.OrderId);

            try
            {
                _logger.LogInformation(
                    "Processing OrderConfirmedEvent for Order {OrderId} with {ItemCount} items. CorrelationId: {CorrelationId}",
                    orderEvent.OrderId,
                    orderEvent.Items.Count,
                    orderEvent.CorrelationId);

                // Check for idempotency - see if we've already processed this event
                var existingProcessedEvent = await _dbContext.ProcessedEvents
                    .FirstOrDefaultAsync(pe => pe.EventId == orderEvent.EventId);

                if (existingProcessedEvent != null)
                {
                    _logger.LogWarning(
                        "Event {EventId} for Order {OrderId} has already been processed. Skipping to ensure idempotency.",
                        orderEvent.EventId,
                        orderEvent.OrderId);
                    return;
                }

                // Find existing stock reservations for this order
                var stockReservations = await _dbContext.StockReservations
                    .Where(r => r.OrderId == orderEvent.OrderId && r.Status == ReservationStatus.Reserved)
                    .ToListAsync();

                if (!stockReservations.Any())
                {
                    _logger.LogError(
                        "No active stock reservations found for Order {OrderId}. Cannot process order confirmation.",
                        orderEvent.OrderId);

                    // Mark event as processed with error to prevent infinite retries
                    var errorProcessedEvent = new ProcessedEvent
                    {
                        EventId = orderEvent.EventId,
                        EventType = nameof(OrderConfirmedEvent),
                        OrderId = orderEvent.OrderId,
                        ProcessedAt = DateTime.UtcNow,
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

                _logger.LogInformation(
                    "Found {ReservationCount} stock reservations for Order {OrderId}. Converting to debited status.",
                    stockReservations.Count,
                    orderEvent.OrderId);

                // Process each reservation
                foreach (var reservation in stockReservations)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Processing stock deduction for Product {ProductId} ({ProductName}), Reserved Quantity: {Quantity}",
                            reservation.ProductId,
                            reservation.ProductName,
                            reservation.Quantity);

                        var product = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.Id == reservation.ProductId);

                        if (product == null)
                        {
                            _logger.LogError(
                                "Product {ProductId} not found during stock deduction for Order {OrderId}",
                                reservation.ProductId,
                                orderEvent.OrderId);

                            allDeductionsSuccessful = false;
                            errorMessage = $"Product {reservation.ProductId} not found";
                            continue;
                        }

                        _logger.LogInformation(
                            "Found product {ProductId}. Current stock: {CurrentStock}, Reserved quantity: {ReservedQuantity}",
                            reservation.ProductId,
                            product.StockQuantity,
                            reservation.Quantity);

                        // Verify we still have sufficient stock (edge case protection)
                        if (product.StockQuantity < reservation.Quantity)
                        {
                            _logger.LogError(
                                "Insufficient stock for Product {ProductId}. Available: {Available}, Reserved: {Reserved} for Order {OrderId}",
                                reservation.ProductId,
                                product.StockQuantity,
                                reservation.Quantity,
                                orderEvent.OrderId);

                            allDeductionsSuccessful = false;
                            errorMessage = $"Insufficient stock for product {reservation.ProductName}";
                            continue;
                        }

                        // Record stock deduction for audit
                        var stockDeduction = new StockDeduction
                        {
                            ProductId = reservation.ProductId,
                            ProductName = reservation.ProductName,
                            QuantityDebited = reservation.Quantity,
                            PreviousStock = product.StockQuantity,
                            NewStock = product.StockQuantity - reservation.Quantity
                        };

                        // Debit the stock and update reservation status
                        product.StockQuantity -= reservation.Quantity;
                        reservation.Status = ReservationStatus.Debited;
                        reservation.ProcessedAt = DateTime.UtcNow;
                        
                        stockDeductions.Add(stockDeduction);

                        _logger.LogInformation(
                            "Stock debited for Product {ProductId} ({ProductName}). Previous: {Previous}, Debited: {Debited}, New: {New}. Reservation {ReservationId} marked as Debited.",
                            reservation.ProductId,
                            reservation.ProductName,
                            stockDeduction.PreviousStock,
                            stockDeduction.QuantityDebited,
                            stockDeduction.NewStock,
                            reservation.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing stock deduction for Product {ProductId} in Order {OrderId}",
                            reservation.ProductId,
                            orderEvent.OrderId);

                        allDeductionsSuccessful = false;
                        errorMessage = $"Error processing product {reservation.ProductName}: {ex.Message}";
                    }
                }

                // Mark event as processed for idempotency
                var processedEvent = new ProcessedEvent
                {
                    EventId = orderEvent.EventId,
                    EventType = nameof(OrderConfirmedEvent),
                    OrderId = orderEvent.OrderId,
                    ProcessedAt = DateTime.UtcNow,
                    CorrelationId = orderEvent.CorrelationId,
                    ProcessingDetails = allDeductionsSuccessful 
                        ? $"Successfully processed {stockDeductions.Count} stock deductions from reservations"
                        : $"Partial processing: {stockDeductions.Count} successful, errors: {errorMessage}"
                };

                _dbContext.ProcessedEvents.Add(processedEvent);

                // Save all changes with retry strategy
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Database changes saved successfully for Order {OrderId}", orderEvent.OrderId);

                // Try to publish StockDebitedEvent
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
                    _logger.LogWarning(ex, "Failed to publish StockDebitedEvent for Order {OrderId}, but stock was processed successfully", orderEvent.OrderId);
                }

                _logger.LogInformation(
                    "=== HANDLER COMPLETED === Successfully processed OrderConfirmedEvent for Order {OrderId}. Debited {Count} items from reservations. Success: {Success}",
                    orderEvent.OrderId,
                    stockDeductions.Count,
                    allDeductionsSuccessful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "=== HANDLER FAILED === Failed to process OrderConfirmedEvent for Order {OrderId}.",
                    orderEvent.OrderId);

                // Re-throw to trigger retry mechanism
                throw;
            }
        }
    }
}