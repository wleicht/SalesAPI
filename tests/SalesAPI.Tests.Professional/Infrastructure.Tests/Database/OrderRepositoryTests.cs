using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Bogus;
using SalesApi.Infrastructure.Data;
using SalesApi.Domain.Entities;
using SalesAPI.Tests.Professional.TestInfrastructure.Database;
using SalesAPI.Tests.Professional.TestInfrastructure.Configuration;
using SalesAPI.Tests.Professional.TestInfrastructure.Fixtures;
using Xunit;

namespace SalesAPI.Tests.Professional.Infrastructure.Tests.Database
{
    /// <summary>
    /// Comprehensive tests for OrderRepository operations.
    /// Tests cover basic CRUD operations, complex queries, and performance scenarios.
    /// </summary>
    [Collection("Database Collection")]
    public class OrderRepositoryTests : IAsyncLifetime
    {
        private readonly DatabaseFixture _fixture;
        private readonly SalesDbContext _context;
        private readonly Faker _faker;
        private readonly ILogger<OrderRepositoryTests> _logger;

        public OrderRepositoryTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
            _context = _fixture.DatabaseFactory.CreateSalesDbContext();
            _faker = new Faker();
            _logger = _fixture.LoggerFactory.CreateLogger<OrderRepositoryTests>();
        }

        public async Task InitializeAsync()
        {
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        [Fact]
        public async Task CreateOrder_WithValidData_ShouldPersistCorrectly()
        {
            // Arrange
            var order = CreateTestOrder();
            var productId = Guid.NewGuid();
            var productName = _faker.Commerce.ProductName();
            var quantity = _faker.Random.Int(TestConstants.TestData.MinQuantity, TestConstants.TestData.MaxQuantity);
            var unitPrice = _faker.Random.Decimal(TestConstants.TestData.MinPrice, TestConstants.TestData.MaxPrice);
            
            order.AddItem(productId, productName, quantity, unitPrice, "test-user");

            // Act
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Assert
            var savedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            savedOrder.Should().NotBeNull();
            savedOrder!.Id.Should().Be(order.Id);
            savedOrder.Items.Should().HaveCount(1);
            savedOrder.Items.First().ProductName.Should().Be(productName);
        }

        [Fact]
        public async Task GetOrdersByCustomer_WithMultipleOrders_ShouldReturnCorrectOrders()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var orders = new[]
            {
                CreateTestOrder(customerId),
                CreateTestOrder(customerId),
                CreateTestOrder() // Different customer
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            // Act
            var customerOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();

            // Assert
            customerOrders.Should().HaveCount(2);
            customerOrders.Should().AllSatisfy(o => o.CustomerId.Should().Be(customerId));
        }

        [Fact]
        public async Task GetOrdersWithItems_ShouldLoadItemsCorrectly()
        {
            // Arrange
            var order = CreateTestOrder();
            
            // Add multiple items using domain method
            for (int i = 0; i < TestConstants.TestData.AlternativeQuantity; i++)
            {
                var productId = Guid.NewGuid();
                var productName = _faker.Commerce.ProductName();
                var quantity = _faker.Random.Int(TestConstants.TestData.MinQuantity, TestConstants.TestData.MaxQuantity);
                var unitPrice = _faker.Random.Decimal(TestConstants.TestData.MinPrice, TestConstants.TestData.MaxPrice);
                
                order.AddItem(productId, productName, quantity, unitPrice, "test-user");
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var orderWithItems = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            // Assert
            orderWithItems.Should().NotBeNull();
            orderWithItems!.Items.Should().HaveCount(TestConstants.TestData.AlternativeQuantity);
        }

        [Fact]
        public async Task UpdateOrder_ShouldPersistChanges()
        {
            // Arrange
            var order = CreateTestOrder();
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            order.AddItem(Guid.NewGuid(), "Test Product", TestConstants.TestData.SampleQuantity, TestConstants.TestData.SamplePrice1, "test-user");
            await _context.SaveChangesAsync();

            // Assert
            var updatedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);
                
            updatedOrder.Should().NotBeNull();
            updatedOrder!.Items.Should().HaveCount(1);
            updatedOrder.TotalAmount.Should().Be(TestConstants.TestData.SampleQuantity * TestConstants.TestData.SamplePrice1);
        }

        [Fact]
        public async Task DeleteOrder_ShouldRemoveFromDatabase()
        {
            // Arrange
            var order = CreateTestOrder();
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            // Assert
            var deletedOrder = await _context.Orders.FindAsync(order.Id);
            deletedOrder.Should().BeNull();
        }

        [Fact]
        public async Task BulkInsert_ManyOrders_ShouldPerformEfficiently()
        {
            // Arrange
            var orders = Enumerable.Range(1, TestConstants.Performance.BulkInsertRecords)
                .Select(_ => CreateTestOrder())
                .ToList();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Bulk insert
            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            stopwatch.Stop();

            // Assert
            var savedCount = await _context.Orders.CountAsync();
            savedCount.Should().Be(TestConstants.Performance.BulkInsertRecords);
            
            // Performance assertion - should complete quickly
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(TestConstants.Performance.MaxExecutionTimeMs);
        }

        [Fact]
        public async Task OrderWithComplexCalculations_ShouldPersistCorrectly()
        {
            // Arrange
            var order = CreateTestOrder();
            
            // Add items using domain methods
            order.AddItem(
                Guid.NewGuid(), 
                "Complex Product 1", 
                TestConstants.TestData.SampleQuantity, 
                TestConstants.TestData.SamplePrice1, 
                "test-user");
                
            order.AddItem(
                Guid.NewGuid(), 
                "Complex Product 2", 
                TestConstants.TestData.AlternativeQuantity, 
                TestConstants.TestData.SamplePrice2, 
                "test-user");

            // Act
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Assert
            var savedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            savedOrder.Should().NotBeNull();
            savedOrder!.Items.Should().HaveCount(2);
            
            var item1 = savedOrder.Items.First(i => i.ProductName == "Complex Product 1");
            item1.TotalPrice.Should().Be(TestConstants.ExpectedResults.ComplexPrice1Total);
            
            var item2 = savedOrder.Items.First(i => i.ProductName == "Complex Product 2");
            item2.TotalPrice.Should().Be(TestConstants.ExpectedResults.ComplexPrice2Total);
            
            savedOrder.TotalAmount.Should().Be(TestConstants.ExpectedResults.ComplexGrandTotal);
        }

        #region Helper Methods

        private Order CreateTestOrder(Guid? customerId = null)
        {
            return new Order(
                customerId ?? Guid.NewGuid(),
                "test-user"
            );
        }

        #endregion
    }
}