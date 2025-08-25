using System;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using SalesApi.Models;
using Xunit;

namespace SalesApi.Tests.Models
{
    /// <summary>
    /// Unit tests for Order business logic and calculations.
    /// Tests core business rules without external dependencies.
    /// </summary>
    public class OrderTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Act
            var order = new Order();

            // Assert
            order.Id.Should().BeEmpty(); // Id is not auto-generated in constructor
            order.CreatedAt.Should().Be(DateTime.MinValue); // CreatedAt is not auto-set in constructor
            order.Status.Should().Be("Pending");
            order.Items.Should().NotBeNull();
            order.Items.Should().BeEmpty();
            order.TotalAmount.Should().Be(0);
        }

        [Fact]
        public void SetId_ShouldMaintainValue()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Order();

            // Act
            order.Id = orderId;

            // Assert
            order.Id.Should().Be(orderId);
        }

        [Fact]
        public void SetCreatedAt_ShouldMaintainValue()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var order = new Order();

            // Act
            order.CreatedAt = createdAt;

            // Assert
            order.CreatedAt.Should().Be(createdAt);
        }

        [Fact]
        public void AddItem_ShouldAddToItemsCollection()
        {
            // Arrange
            var order = new Order();
            var item = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 2,
                UnitPrice = 15.50m
            };

            // Act
            order.Items.Add(item);

            // Assert
            order.Items.Should().HaveCount(1);
            order.Items.First().Should().Be(item);
        }

        [Fact]
        public void CalculateTotalAmount_ShouldSumAllItems()
        {
            // Arrange
            var order = new Order();
            
            order.Items.Add(new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 1",
                Quantity = 2,
                UnitPrice = 10.00m
            });
            
            order.Items.Add(new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product 2", 
                Quantity = 3,
                UnitPrice = 15.00m
            });

            // Act
            var totalAmount = order.Items.Sum(i => i.TotalPrice);
            order.TotalAmount = totalAmount;

            // Assert
            order.TotalAmount.Should().Be(65.00m); // (2 * 10.00) + (3 * 15.00) = 20 + 45 = 65
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Confirmed")]
        [InlineData("Cancelled")]
        [InlineData("Processing")]
        public void Status_WhenSet_ShouldMaintainValue(string status)
        {
            // Arrange
            var order = new Order();

            // Act
            order.Status = status;

            // Assert
            order.Status.Should().Be(status);
        }

        [Fact]
        public void Order_WithMultipleItems_ShouldCalculateCorrectTotal()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow
            };

            var items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    Quantity = 1,
                    UnitPrice = 999.99m
                },
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Mouse",
                    Quantity = 2,
                    UnitPrice = 25.50m
                },
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Keyboard",
                    Quantity = 1,
                    UnitPrice = 75.00m
                }
            };

            // Act
            foreach (var item in items)
            {
                order.Items.Add(item);
            }
            
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // Assert
            order.TotalAmount.Should().Be(1125.99m); // 999.99 + (2 * 25.50) + 75.00 = 999.99 + 51.00 + 75.00
            order.Items.Should().HaveCount(3);
        }

        [Fact]
        public void Order_WithNoItems_ShouldHaveZeroTotal()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // Assert
            order.TotalAmount.Should().Be(0m);
            order.Items.Should().BeEmpty();
        }

        [Fact]
        public void Order_WithSingleHighValueItem_ShouldCalculateCorrectly()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed",
                CreatedAt = DateTime.UtcNow
            };

            var expensiveItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Enterprise Server",
                Quantity = 1,
                UnitPrice = 10000.00m
            };

            // Act
            order.Items.Add(expensiveItem);
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

            // Assert
            order.TotalAmount.Should().Be(10000.00m);
            order.Items.Should().HaveCount(1);
        }
    }
}