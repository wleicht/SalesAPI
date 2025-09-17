using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesApi.Domain.Entities;
using SalesApi.Infrastructure.Data;
using InventoryApi.Domain.Entities;
using InventoryApi.Persistence;
using SalesAPI.Tests.Professional.TestInfrastructure.Database;
using SalesAPI.Tests.Professional.TestInfrastructure.Messaging;
using Xunit;
using Bogus;

namespace SalesAPI.Tests.Professional.Integration.Tests.OrderFlow
{
    /// <summary>
    /// Integration tests for complete order processing flow.
    /// Tests the interaction between database operations and messaging without complex API calls.
    /// Uses professional domain entities throughout.
    /// </summary>
    public class OrderProcessingIntegrationTests : IAsyncLifetime
    {
        private readonly TestDatabaseFactory _databaseFactory;
        private readonly TestMessagingFactory _messagingFactory;
        private readonly Faker _faker;
        
        private SalesDbContext _salesContext = null!;
        private InventoryDbContext _inventoryContext = null!;

        public OrderProcessingIntegrationTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<TestDatabaseFactory>();
            _databaseFactory = new TestDatabaseFactory(logger);
            _messagingFactory = new TestMessagingFactory("OrderProcessingIntegration");
            _faker = new Faker();
        }

        public async Task InitializeAsync()
        {
            // Initialize database contexts for the test
            _salesContext = _databaseFactory.CreateInMemoryContext<SalesDbContext>(
                $"OrderProcessingIntegration_Sales_{Guid.NewGuid():N}");
            _inventoryContext = _databaseFactory.CreateInMemoryContext<InventoryDbContext>(
                $"OrderProcessingIntegration_Inventory_{Guid.NewGuid():N}");

            await _salesContext.Database.EnsureCreatedAsync();
            await _inventoryContext.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _salesContext.DisposeAsync();
            await _inventoryContext.DisposeAsync();
            _databaseFactory.Dispose();
            _messagingFactory.Dispose();
        }

        [Fact]
        public async Task CompleteOrderFlow_WithAvailableStock_ShouldCreateOrderAndReserveStock()
        {
            // Arrange - Setup test scenario with professional domain entities
            var scenario = await SetupOrderScenarioAsync();
            var messagingCollector = _messagingFactory.CreateMessageCollector();

            // Act - Create order using domain entity
            var order = new Order(scenario.CustomerId, "test-user");
            order.AddItem(scenario.ProductId, scenario.ProductName, scenario.OrderQuantity, scenario.UnitPrice, "test-user");
            order.Confirm("test-user");
            
            _salesContext.Orders.Add(order);
            await _salesContext.SaveChangesAsync();

            // Simulate inventory reservation processing
            await ProcessInventoryReservationAsync(order, messagingCollector);

            // Assert - Verify complete flow
            await VerifyOrderCreatedAsync(order);
            await VerifyStockReservedAsync(scenario, order.Id); // Pass the real order ID
            VerifyMessagesPublished(messagingCollector, scenario, order.Id);
        }

        [Fact]
        public async Task OrderCancellation_ShouldReleaseStockReservation()
        {
            // Arrange - Create order with stock reservation
            var scenario = await SetupOrderScenarioAsync();
            var order = await CreateTestOrderWithReservationAsync(scenario);
            var messagingCollector = _messagingFactory.CreateMessageCollector();

            // Act - Cancel order using domain method
            order.Cancel("test-user", "Customer request");
            await _salesContext.SaveChangesAsync();

            // Simulate inventory release processing
            await ProcessInventoryCancellationAsync(order, messagingCollector);

            // Assert - Verify stock was released
            var reservation = await _inventoryContext.StockReservations
                .FirstOrDefaultAsync(sr => sr.OrderId == order.Id);
            
            reservation.Should().NotBeNull();
            reservation!.Status.Should().Be(ReservationStatus.Released);

            var product = await _inventoryContext.Products.FindAsync(scenario.ProductId);
            product!.StockQuantity.Should().Be(scenario.InitialStock); // Stock restored
        }

        [Fact]
        public async Task MultipleOrdersForSameProduct_ShouldHandleCorrectly()
        {
            // Arrange - Setup product with limited stock using domain entity
            var product = new Product(
                "Limited Stock Product",
                "Product with limited availability",
                50.00m,
                10, // Only 10 available
                "test-user");
            
            _inventoryContext.Products.Add(product);
            await _inventoryContext.SaveChangesAsync();

            var messagingCollector = _messagingFactory.CreateMessageCollector();

            // Act - Create multiple orders
            var orders = new List<Order>();
            for (int i = 1; i <= 3; i++)
            {
                var order = new Order(Guid.NewGuid(), "test-user");
                order.AddItem(product.Id, product.Name, 4, product.Price, "test-user"); // Each order wants 4 items
                order.Confirm("test-user");
                
                orders.Add(order);
                
                _salesContext.Orders.Add(order);
                await _salesContext.SaveChangesAsync();

                // Process reservation for each order
                await ProcessInventoryReservationAsync(order, messagingCollector);
            }

            // Assert - Check final state
            var updatedProduct = await _inventoryContext.Products.FindAsync(product.Id);
            updatedProduct!.StockQuantity.Should().BeLessOrEqualTo(10);

            var reservations = await _inventoryContext.StockReservations
                .Where(sr => sr.ProductId == product.Id)
                .ToListAsync();

            var totalReserved = reservations.Where(r => r.Status == ReservationStatus.Debited)
                                           .Sum(r => r.Quantity);
            
            totalReserved.Should().BeLessOrEqualTo(10); // Cannot exceed available stock
        }

        [Fact] 
        public async Task OrderWithMultipleProducts_ShouldHandleEachProductCorrectly()
        {
            // Arrange - Setup multiple products using domain entities
            var products = new[]
            {
                new Product("Product A", "First test product", 10.00m, 100, "test-user"),
                new Product("Product B", "Second test product", 20.00m, 50, "test-user"),
                new Product("Product C", "Third test product", 30.00m, 25, "test-user")
            };

            _inventoryContext.Products.AddRange(products);
            await _inventoryContext.SaveChangesAsync();

            var messagingCollector = _messagingFactory.CreateMessageCollector();

            // Act - Create order with multiple products using domain methods
            var order = new Order(Guid.NewGuid(), "test-user");

            // Add items to the order using domain methods
            foreach (var product in products)
            {
                order.AddItem(product.Id, product.Name, 5, product.Price, "test-user");
            }

            order.Confirm("test-user");

            _salesContext.Orders.Add(order);
            await _salesContext.SaveChangesAsync();

            // Process inventory for each product
            await ProcessInventoryReservationAsync(order, messagingCollector);

            // Assert - Verify each product was processed
            foreach (var product in products)
            {
                var reservation = await _inventoryContext.StockReservations
                    .FirstOrDefaultAsync(sr => sr.OrderId == order.Id && sr.ProductId == product.Id);

                reservation.Should().NotBeNull();
                reservation!.Status.Should().Be(ReservationStatus.Debited);
                reservation.Quantity.Should().Be(5);

                // Re-fetch product to get updated stock
                var updatedProduct = await _inventoryContext.Products.FindAsync(product.Id);
                updatedProduct.Should().NotBeNull();
                updatedProduct!.StockQuantity.Should().BeLessOrEqualTo(product.StockQuantity);
            }
        }

        #region Helper Methods

        private async Task<OrderScenario> SetupOrderScenarioAsync()
        {
            var scenario = new OrderScenario
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                ProductName = _faker.Commerce.ProductName(),
                InitialStock = 100,
                OrderQuantity = 5,
                UnitPrice = 99.99m
            };

            // Create product using domain entity
            var product = new Product(
                scenario.ProductName,
                _faker.Commerce.ProductDescription(),
                scenario.UnitPrice,
                scenario.InitialStock,
                "test-user");

            // Set the ID using reflection for testing purposes
            var idProperty = typeof(Product).GetProperty("Id");
            idProperty?.SetValue(product, scenario.ProductId);

            _inventoryContext.Products.Add(product);
            await _inventoryContext.SaveChangesAsync();

            return scenario;
        }

        private async Task<Order> CreateTestOrderWithReservationAsync(OrderScenario scenario)
        {
            var order = new Order(scenario.CustomerId, "test-user");
            order.AddItem(scenario.ProductId, scenario.ProductName, scenario.OrderQuantity, scenario.UnitPrice, "test-user");
            order.Confirm("test-user");

            _salesContext.Orders.Add(order);
            await _salesContext.SaveChangesAsync();

            // Create reservation using domain entity
            var reservation = new StockReservation
            {
                OrderId = order.Id,
                ProductId = scenario.ProductId,
                ProductName = scenario.ProductName,
                Quantity = scenario.OrderQuantity,
                Status = ReservationStatus.Debited
            };

            _inventoryContext.StockReservations.Add(reservation);

            // Use domain method to remove stock
            var product = await _inventoryContext.Products.FindAsync(scenario.ProductId);
            product!.RemoveStock(scenario.OrderQuantity, "test-user");

            await _inventoryContext.SaveChangesAsync();

            return order;
        }

        private async Task ProcessInventoryReservationAsync(Order order, MessageCollector collector)
        {
            foreach (var item in order.Items)
            {
                // Use domain methods for stock operations
                var product = await _inventoryContext.Products.FindAsync(item.ProductId);
                if (product != null && product.HasSufficientStock(item.Quantity))
                {
                    product.ReserveStock(item.Quantity, "integration-test");

                    // Create reservation using domain entity
                    var reservation = new StockReservation
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Status = ReservationStatus.Debited
                    };

                    _inventoryContext.StockReservations.Add(reservation);

                    // Record message
                    collector.RecordMessage(new StockProcessedMessage
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Success = true
                    });
                }
            }

            await _inventoryContext.SaveChangesAsync();
        }

        private async Task ProcessInventoryCancellationAsync(Order order, MessageCollector collector)
        {
            foreach (var item in order.Items)
            {
                // Use domain methods for stock restoration
                var product = await _inventoryContext.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.ReleaseReservedStock(item.Quantity, "integration-test");

                    // Update reservation
                    var reservation = await _inventoryContext.StockReservations
                        .FirstOrDefaultAsync(sr => sr.OrderId == order.Id && sr.ProductId == item.ProductId);
                    
                    if (reservation != null)
                    {
                        reservation.Status = ReservationStatus.Released;
                    }

                    // Record message
                    collector.RecordMessage(new StockProcessedMessage
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Success = true,
                        Action = "Released"
                    });
                }
            }

            await _inventoryContext.SaveChangesAsync();
        }

        private async Task VerifyOrderCreatedAsync(Order order)
        {
            var savedOrder = await _salesContext.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            savedOrder.Should().NotBeNull();
            savedOrder!.Status.Should().Be(OrderStatus.Confirmed);
            savedOrder.Items.Should().HaveCount(order.Items.Count);
        }

        private async Task VerifyStockReservedAsync(OrderScenario scenario, Guid actualOrderId)
        {
            var product = await _inventoryContext.Products.FindAsync(scenario.ProductId);
            product.Should().NotBeNull();
            product!.StockQuantity.Should().Be(scenario.InitialStock - scenario.OrderQuantity);

            var reservation = await _inventoryContext.StockReservations
                .FirstOrDefaultAsync(sr => sr.OrderId == actualOrderId);
            reservation.Should().NotBeNull();
            reservation!.Status.Should().Be(ReservationStatus.Debited);
            reservation.Quantity.Should().Be(scenario.OrderQuantity);
        }

        private void VerifyMessagesPublished(MessageCollector collector, OrderScenario scenario, Guid actualOrderId)
        {
            var messages = collector.GetMessages<StockProcessedMessage>();
            messages.Should().HaveCount(1);
            
            var message = messages.First();
            message.OrderId.Should().Be(actualOrderId);
            message.ProductId.Should().Be(scenario.ProductId);
            message.Success.Should().BeTrue();
        }

        #endregion

        #region Test Data Classes

        private class OrderScenario
        {
            public Guid OrderId { get; set; }
            public Guid ProductId { get; set; }
            public Guid CustomerId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int InitialStock { get; set; }
            public int OrderQuantity { get; set; }
            public decimal UnitPrice { get; set; }
        }

        private class StockProcessedMessage
        {
            public Guid OrderId { get; set; }
            public Guid ProductId { get; set; }
            public int Quantity { get; set; }
            public bool Success { get; set; }
            public string Action { get; set; } = "Reserved";
        }

        #endregion
    }
}