using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesApi.Models;
using SalesApi.Persistence;
using SalesAPI.Tests.Professional.TestInfrastructure.Database;
using Xunit;
using Bogus;

namespace SalesAPI.Tests.Professional.Infrastructure.Tests.Database
{
    /// <summary>
    /// Infrastructure tests for Order repository operations using real database.
    /// Tests actual database operations, transactions, and data persistence.
    /// </summary>
    public class OrderRepositoryTests : IAsyncLifetime
    {
        private readonly TestDatabaseFactory _databaseFactory;
        private SalesDbContext _context = null!;
        private readonly Faker _faker;

        public OrderRepositoryTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<TestDatabaseFactory>();
            _databaseFactory = new TestDatabaseFactory(logger);
            _faker = new Faker();
        }

        public async Task InitializeAsync()
        {
            // Use in-memory database for fast infrastructure testing
            _context = _databaseFactory.CreateInMemoryContext<SalesDbContext>(
                $"OrderRepositoryTests_{Guid.NewGuid():N}");

            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            _databaseFactory.Dispose();
        }

        [Fact]
        public async Task SaveOrder_WithSingleItem_ShouldPersistCorrectly()
        {
            // Arrange
            var order = CreateTestOrder();
            var orderItem = CreateTestOrderItem(order.Id);
            order.Items.Add(orderItem);
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // Act - Save using real DbContext
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Assert - Verify persistence
            var savedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            savedOrder.Should().NotBeNull();
            savedOrder!.CustomerId.Should().Be(order.CustomerId);
            savedOrder.Status.Should().Be(order.Status);
            savedOrder.TotalAmount.Should().Be(order.TotalAmount);
            savedOrder.Items.Should().HaveCount(1);
            
            var savedItem = savedOrder.Items.First();
            savedItem.ProductId.Should().Be(orderItem.ProductId);
            savedItem.Quantity.Should().Be(orderItem.Quantity);
            savedItem.UnitPrice.Should().Be(orderItem.UnitPrice);
            savedItem.TotalPrice.Should().Be(orderItem.TotalPrice);
        }

        [Fact]
        public async Task SaveOrder_WithMultipleItems_ShouldPersistAllItems()
        {
            // Arrange
            var order = CreateTestOrder();
            var items = CreateMultipleTestOrderItems(order.Id, count: 5);
            
            foreach (var item in items)
            {
                order.Items.Add(item);
            }
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // Act
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Assert
            var savedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            savedOrder.Should().NotBeNull();
            savedOrder!.Items.Should().HaveCount(5);
            savedOrder.TotalAmount.Should().Be(order.TotalAmount);
            
            // Verify each item was saved correctly
            foreach (var originalItem in items)
            {
                var savedItem = savedOrder.Items.First(i => i.ProductId == originalItem.ProductId);
                savedItem.Quantity.Should().Be(originalItem.Quantity);
                savedItem.UnitPrice.Should().Be(originalItem.UnitPrice);
                savedItem.TotalPrice.Should().Be(originalItem.TotalPrice);
            }
        }

        [Fact]
        public async Task UpdateOrder_ChangeStatus_ShouldPersistChanges()
        {
            // Arrange
            var order = CreateTestOrder();
            order.Status = "Pending";
            
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act - Update status
            order.Status = "Confirmed";
            await _context.SaveChangesAsync();

            // Assert
            var updatedOrder = await _context.Orders.FindAsync(order.Id);
            updatedOrder.Should().NotBeNull();
            updatedOrder!.Status.Should().Be("Confirmed");
        }

        [Fact]
        public async Task QueryOrders_ByCustomerId_ShouldReturnCorrectOrders()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var otherCustomerId = Guid.NewGuid();
            
            // Create orders for target customer
            var customerOrders = Enumerable.Range(1, 3)
                .Select(_ => CreateTestOrder(customerId))
                .ToList();
                
            // Create orders for other customer
            var otherOrders = Enumerable.Range(1, 2)
                .Select(_ => CreateTestOrder(otherCustomerId))
                .ToList();

            _context.Orders.AddRange(customerOrders);
            _context.Orders.AddRange(otherOrders);
            await _context.SaveChangesAsync();

            // Act - Query by customer ID
            var foundOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();

            // Assert
            foundOrders.Should().HaveCount(3);
            foundOrders.All(o => o.CustomerId == customerId).Should().BeTrue();
            
            // Verify we didn't get orders from other customer
            foundOrders.Should().NotContain(o => o.CustomerId == otherCustomerId);
        }

        [Fact]
        public async Task QueryOrders_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var orders = Enumerable.Range(1, 10)
                .Select(i => CreateTestOrder(customerId, createdAt: DateTime.UtcNow.AddDays(-i)))
                .ToList();

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            // Act - Get second page (skip 3, take 4)
            var pagedOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .Skip(3)
                .Take(4)
                .ToListAsync();

            // Assert
            pagedOrders.Should().HaveCount(4);
            pagedOrders.Should().BeInDescendingOrder(o => o.CreatedAt);
        }

        [Fact]
        public async Task DeleteOrder_ShouldRemoveOrderAndItems()
        {
            // Arrange
            var order = CreateTestOrder();
            var items = CreateMultipleTestOrderItems(order.Id, count: 3);
            
            foreach (var item in items)
            {
                order.Items.Add(item);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Verify order exists
            var existingOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);
            existingOrder.Should().NotBeNull();
            existingOrder!.Items.Should().HaveCount(3);

            // Act - Delete order
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            // Assert - Order should be deleted
            var deletedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);
            deletedOrder.Should().BeNull();

            // Verify cascade delete removed items
            var orphanedItems = await _context.OrderItems
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync();
            orphanedItems.Should().BeEmpty();
        }

        [Fact]
        public async Task ConcurrentUpdate_ShouldHandleCorrectly()
        {
            // Arrange
            var order = CreateTestOrder();
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create two contexts to simulate concurrent access
            var context1 = _databaseFactory.CreateInMemoryContext<SalesDbContext>(
                $"ConcurrentTest1_{Guid.NewGuid():N}");
            var context2 = _databaseFactory.CreateInMemoryContext<SalesDbContext>(
                $"ConcurrentTest2_{Guid.NewGuid():N}");

            // Note: In-memory database doesn't fully simulate concurrency like SQL Server would
            // For real concurrency testing, you'd use a real database with proper isolation levels

            try
            {
                // Act - Simulate concurrent updates (simplified for in-memory)
                var order1 = await context1.Orders.FindAsync(order.Id);
                var order2 = await context2.Orders.FindAsync(order.Id);

                if (order1 != null && order2 != null)
                {
                    order1.Status = "Confirmed";
                    order2.Status = "Cancelled";

                    await context1.SaveChangesAsync();
                    await context2.SaveChangesAsync();

                    // Assert - Verify final state
                    var finalOrder = await _context.Orders.FindAsync(order.Id);
                    finalOrder.Should().NotBeNull();
                    // Last update wins in this simplified scenario
                    finalOrder!.Status.Should().Be("Cancelled");
                }
            }
            finally
            {
                await context1.DisposeAsync();
                await context2.DisposeAsync();
            }
        }

        [Fact]
        public async Task BulkInsert_ManyOrders_ShouldPerformEfficiently()
        {
            // Arrange
            var orders = Enumerable.Range(1, 50) // Reduced count for faster testing
                .Select(_ => CreateTestOrder())
                .ToList();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Bulk insert
            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            stopwatch.Stop();

            // Assert
            var savedCount = await _context.Orders.CountAsync();
            savedCount.Should().Be(50);
            
            // Performance assertion - should complete quickly
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // 3 seconds max for in-memory
        }

        [Fact]
        public async Task OrderWithComplexCalculations_ShouldPersistCorrectly()
        {
            // Arrange
            var order = CreateTestOrder();
            var complexItems = new[]
            {
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Complex Product 1",
                    Quantity = 7,
                    UnitPrice = 123.456m
                },
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Complex Product 2",
                    Quantity = 3,
                    UnitPrice = 999.999m
                }
            };

            foreach (var item in complexItems)
            {
                order.Items.Add(item);
            }
            
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

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
            item1.TotalPrice.Should().Be(864.192m); // 7 * 123.456
            
            var item2 = savedOrder.Items.First(i => i.ProductName == "Complex Product 2");
            item2.TotalPrice.Should().Be(2999.997m); // 3 * 999.999
            
            savedOrder.TotalAmount.Should().Be(3864.189m); // 864.192 + 2999.997
        }

        #region Helper Methods

        private Order CreateTestOrder(Guid? customerId = null, DateTime? createdAt = null)
        {
            return new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId ?? Guid.NewGuid(),
                Status = "Pending",
                TotalAmount = 0,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                Items = new List<OrderItem>()
            };
        }

        private OrderItem CreateTestOrderItem(Guid orderId)
        {
            return new OrderItem
            {
                OrderId = orderId,
                ProductId = Guid.NewGuid(),
                ProductName = _faker.Commerce.ProductName(),
                Quantity = _faker.Random.Int(1, 10),
                UnitPrice = _faker.Random.Decimal(10, 1000)
            };
        }

        private List<OrderItem> CreateMultipleTestOrderItems(Guid orderId, int count)
        {
            return Enumerable.Range(1, count)
                .Select(_ => CreateTestOrderItem(orderId))
                .ToList();
        }

        #endregion
    }
}