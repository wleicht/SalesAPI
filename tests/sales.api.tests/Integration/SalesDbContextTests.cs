using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SalesApi.Models;
using SalesApi.Persistence;
using Xunit;

namespace SalesApi.Tests.Integration
{
    /// <summary>
    /// Integration tests for SalesDbContext with in-memory database.
    /// Tests order processing and data persistence without external dependencies.
    /// </summary>
    public class SalesDbContextTests : IDisposable
    {
        private readonly SalesDbContext _context;

        public SalesDbContextTests()
        {
            var options = new DbContextOptionsBuilder<SalesDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SalesDbContext(options);
        }

        [Fact]
        public async Task AddOrder_ShouldPersistWithItems()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed",
                TotalAmount = 299.98m
            };

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 2,
                UnitPrice = 149.99m
            };

            order.Items.Add(orderItem);

            // Act
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Assert
            var savedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            savedOrder.Should().NotBeNull();
            savedOrder!.Status.Should().Be("Confirmed");
            savedOrder.TotalAmount.Should().Be(299.98m);
            savedOrder.Items.Should().HaveCount(1);
            savedOrder.Items.First().TotalPrice.Should().Be(299.98m);
        }

        [Fact]
        public async Task QueryOrdersByCustomer_ShouldReturnCorrectResults()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var orders = new List<Order>
            {
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = "Confirmed",
                    TotalAmount = 100.00m
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Status = "Pending",
                    TotalAmount = 200.00m
                },
                new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(), // Different customer
                    Status = "Confirmed",
                    TotalAmount = 150.00m
                }
            };

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            // Act
            var customerOrders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();

            // Assert
            customerOrders.Should().HaveCount(2);
            customerOrders.All(o => o.CustomerId == customerId).Should().BeTrue();
            customerOrders.Sum(o => o.TotalAmount).Should().Be(300.00m);
        }

        [Fact]
        public async Task OrderWithMultipleItems_ShouldCalculateCorrectTotal()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed"
            };

            var items = new List<OrderItem>
            {
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 1,
                    UnitPrice = 999.99m
                },
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mouse",
                    Quantity = 2,
                    UnitPrice = 25.50m
                }
            };

            // Add items individually since ICollection doesn't have AddRange
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
            savedOrder!.Items.Should().HaveCount(2);
            savedOrder.TotalAmount.Should().Be(1050.99m); // 999.99 + (2 * 25.50)
            
            var totalFromItems = savedOrder.Items.Sum(i => i.TotalPrice);
            savedOrder.TotalAmount.Should().Be(totalFromItems);
        }

        [Fact]
        public async Task UpdateOrderStatus_ShouldModifyStatus()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Pending",
                TotalAmount = 50.00m
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            order.Status = "Confirmed";
            await _context.SaveChangesAsync();

            // Assert
            var updatedOrder = await _context.Orders.FindAsync(order.Id);
            updatedOrder!.Status.Should().Be("Confirmed");
        }

        [Fact]
        public async Task OrdersPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var orders = Enumerable.Range(1, 10)
                .Select(i => new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = Guid.NewGuid(),
                    Status = "Confirmed",
                    TotalAmount = i * 10.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                })
                .ToList();

            _context.Orders.AddRange(orders);
            await _context.SaveChangesAsync();

            // Act - Get second page (skip 3, take 3)
            var pagedOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip(3)
                .Take(3)
                .ToListAsync();

            // Assert
            pagedOrders.Should().HaveCount(3);
            pagedOrders.Should().BeInDescendingOrder(o => o.CreatedAt);
        }

        [Fact]
        public async Task OrderItemTotalPrice_ShouldCalculateCorrectly()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed"
            };

            var expensiveItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = Guid.NewGuid(),
                ProductName = "Enterprise Software License",
                Quantity = 5,
                UnitPrice = 999.99m
            };

            order.Items.Add(expensiveItem);
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // Act
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Assert
            var savedOrder = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            var item = savedOrder!.Items.First();
            item.TotalPrice.Should().Be(4999.95m); // 5 * 999.99
            savedOrder.TotalAmount.Should().Be(4999.95m);
        }

        [Fact]
        public async Task CascadeDelete_ShouldRemoveOrderItems()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed",
                TotalAmount = 100.00m
            };

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = Guid.NewGuid(),
                ProductName = "Cascade Test Product",
                Quantity = 1,
                UnitPrice = 100.00m
            };

            order.Items.Add(orderItem);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            // Assert
            var deletedOrder = await _context.Orders.FindAsync(order.Id);
            var orphanedItems = await _context.OrderItems
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync();

            deletedOrder.Should().BeNull();
            orphanedItems.Should().BeEmpty(); // Cascade delete should remove items
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}