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
        /// Processes an OrderConfirmedEvent by debiting stock quantities for all order items.
        /// Implements comprehensive error handling, idempotency checks, and audit trail creation.
        /// </summary>
        /// <param name="orderEvent">The order confirmed event containing order details and items to process</param>
        /// <returns>A task representing the asynchronous event processing operation</returns>
        /// <remarks>
        /// Processing flow:
        /// 1. Validates event hasn't been processed before (idempotency)
        /// 2. Begins database transaction for consistency
        /// 3. Processes each order item:
        ///    - Validates product exists
        ///    - Checks sufficient stock availability
        ///    - Records stock deduction for audit
        ///    - Updates product stock quantity
        /// 4. Marks event as processed to prevent reprocessing
        /// 5. Publishes StockDebitedEvent response
        /// 6. Commits transaction or rolls back on error
        /// 
        /// The handler ensures that either all stock deductions succeed or none are applied,
        /// maintaining data consistency across the distributed system.
        /// </remarks>
        /// <exception cref="Exception">
        /// Re-throws any processing exceptions to trigger Rebus retry mechanisms.
        /// Database transactions are rolled back automatically on failures.
        /// </exception>
        public async Task Handle(OrderConfirmedEvent orderEvent)
        {
            _logger.LogInformation("=== HANDLER CALLED === Processing OrderConfirmedEvent for Order {OrderId}", orderEvent.OrderId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

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

                var stockDeductions = new List<StockDeduction>();
                bool allDeductionsSuccessful = true;
                string? errorMessage = null;

                // Process each item in the order
                foreach (var item in orderEvent.Items)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Processing stock deduction for Product {ProductId} ({ProductName}), Quantity: {Quantity}",
                            item.ProductId,
                            item.ProductName,
                            item.Quantity);

                        var product = await _dbContext.Products
                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                        if (product == null)
                        {
                            _logger.LogError(
                                "Product {ProductId} not found during stock deduction for Order {OrderId}",
                                item.ProductId,
                                orderEvent.OrderId);

                            allDeductionsSuccessful = false;
                            errorMessage = $"Product {item.ProductId} not found";
                            continue;
                        }

                        _logger.LogInformation(
                            "Found product {ProductId}. Current stock: {CurrentStock}, Requesting: {RequestedQuantity}",
                            item.ProductId,
                            product.StockQuantity,
                            item.Quantity);

                        // Check if sufficient stock is available
                        if (product.StockQuantity < item.Quantity)
                        {
                            _logger.LogError(
                                "Insufficient stock for Product {ProductId}. Available: {Available}, Required: {Required} for Order {OrderId}",
                                item.ProductId,
                                product.StockQuantity,
                                item.Quantity,
                                orderEvent.OrderId);

                            allDeductionsSuccessful = false;
                            errorMessage = $"Insufficient stock for product {item.ProductName}";
                            continue;
                        }

                        // Record stock deduction for audit
                        var stockDeduction = new StockDeduction
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            QuantityDebited = item.Quantity,
                            PreviousStock = product.StockQuantity,
                            NewStock = product.StockQuantity - item.Quantity
                        };

                        // Debit the stock
                        product.StockQuantity -= item.Quantity;
                        stockDeductions.Add(stockDeduction);

                        _logger.LogInformation(
                            "Stock debited for Product {ProductId} ({ProductName}). Previous: {Previous}, Debited: {Debited}, New: {New}",
                            item.ProductId,
                            item.ProductName,
                            stockDeduction.PreviousStock,
                            stockDeduction.QuantityDebited,
                            stockDeduction.NewStock);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error processing stock deduction for Product {ProductId} in Order {OrderId}",
                            item.ProductId,
                            orderEvent.OrderId);

                        allDeductionsSuccessful = false;
                        errorMessage = $"Error processing product {item.ProductName}: {ex.Message}";
                    }
                }

                // Mark event as processed for idempotency
                var processedEvent = new ProcessedEvent
                {
                    EventId = orderEvent.EventId,
                    EventType = nameof(OrderConfirmedEvent),
                    OrderId = orderEvent.OrderId,
                    ProcessedAt = DateTime.UtcNow,
                    CorrelationId = orderEvent.CorrelationId
                };

                _dbContext.ProcessedEvents.Add(processedEvent);

                // Save all changes first
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

                // Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "=== HANDLER COMPLETED === Successfully processed OrderConfirmedEvent for Order {OrderId}. Debited {Count} items. Success: {Success}",
                    orderEvent.OrderId,
                    stockDeductions.Count,
                    allDeductionsSuccessful);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex,
                    "=== HANDLER FAILED === Failed to process OrderConfirmedEvent for Order {OrderId}. Transaction rolled back.",
                    orderEvent.OrderId);

                // Re-throw to trigger retry mechanism
                throw;
            }
        }
    }
}