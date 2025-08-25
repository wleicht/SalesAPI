using System;
using FluentAssertions;
using SalesApi.Models;
using Xunit;

namespace SalesApi.Tests.Models
{
    /// <summary>
    /// Unit tests for OrderItem business logic and calculations.
    /// Tests price calculations and business rules.
    /// </summary>
    public class OrderItemTests
    {
        [Fact]
        public void TotalPrice_ShouldCalculateCorrectly()
        {
            // Arrange
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 3,
                UnitPrice = 25.50m
            };

            // Act & Assert
            orderItem.TotalPrice.Should().Be(76.50m); // 3 * 25.50
        }

        [Theory]
        [InlineData(1, 10.00, 10.00)]
        [InlineData(2, 15.50, 31.00)]
        [InlineData(5, 9.99, 49.95)]
        [InlineData(10, 100.00, 1000.00)]
        [InlineData(100, 0.99, 99.00)]
        public void TotalPrice_WithVariousQuantitiesAndPrices_ShouldCalculateCorrectly(
            int quantity, decimal unitPrice, decimal expectedTotal)
        {
            // Arrange
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = quantity,
                UnitPrice = unitPrice
            };

            // Act & Assert
            orderItem.TotalPrice.Should().Be(expectedTotal);
        }

        [Fact]
        public void TotalPrice_WithZeroQuantity_ShouldBeZero()
        {
            // Arrange
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 0,
                UnitPrice = 100.00m
            };

            // Act & Assert
            orderItem.TotalPrice.Should().Be(0m);
        }

        [Fact]
        public void TotalPrice_WithZeroPrice_ShouldBeZero()
        {
            // Arrange
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Free Product",
                Quantity = 5,
                UnitPrice = 0m
            };

            // Act & Assert
            orderItem.TotalPrice.Should().Be(0m);
        }

        [Fact]
        public void OrderItem_WithHighPrecisionDecimal_ShouldMaintainPrecision()
        {
            // Arrange
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Precision Product",
                Quantity = 3,
                UnitPrice = 33.333333m
            };

            // Act & Assert
            orderItem.TotalPrice.Should().Be(99.999999m); // 3 * 33.333333
        }

        [Fact]
        public void SetOrderId_ShouldMaintainValue()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var orderItem = new OrderItem
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 1,
                UnitPrice = 10.00m
            };

            // Act
            orderItem.OrderId = orderId;

            // Assert
            orderItem.OrderId.Should().Be(orderId);
        }

        [Fact]
        public void CreateOrderItem_WithAllProperties_ShouldBeValid()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var productName = "Premium Laptop";
            var quantity = 2;
            var unitPrice = 1299.99m;

            // Act
            var orderItem = new OrderItem
            {
                OrderId = orderId,
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                UnitPrice = unitPrice
            };

            // Assert
            orderItem.OrderId.Should().Be(orderId);
            orderItem.ProductId.Should().Be(productId);
            orderItem.ProductName.Should().Be(productName);
            orderItem.Quantity.Should().Be(quantity);
            orderItem.UnitPrice.Should().Be(unitPrice);
            orderItem.TotalPrice.Should().Be(2599.98m); // 2 * 1299.99
        }

        [Fact]
        public void OrderItem_BusinessInvariants_ShouldBeValid()
        {
            // Arrange & Act
            var orderItem = new OrderItem
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Business Product",
                Quantity = 5,
                UnitPrice = 49.99m
            };

            // Assert business invariants
            orderItem.OrderId.Should().NotBeEmpty("OrderId should be a valid GUID");
            orderItem.ProductId.Should().NotBeEmpty("ProductId should be a valid GUID");
            orderItem.ProductName.Should().NotBeNullOrWhiteSpace("ProductName should have a value");
            orderItem.Quantity.Should().BePositive("Quantity should be positive for valid order items");
            orderItem.UnitPrice.Should().BeGreaterOrEqualTo(0, "UnitPrice should be non-negative");
            orderItem.TotalPrice.Should().Be(orderItem.Quantity * orderItem.UnitPrice, 
                "TotalPrice should equal Quantity * UnitPrice");
        }
    }
}