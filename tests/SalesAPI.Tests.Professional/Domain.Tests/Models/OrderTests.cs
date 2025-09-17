using FluentAssertions;
using SalesApi.Domain.Entities;
using SalesAPI.Tests.Professional.TestInfrastructure.Builders;
using Xunit;

namespace SalesAPI.Tests.Professional.Domain.Tests.Models
{
    /// <summary>
    /// Pure domain logic tests for Order entity.
    /// These tests focus on business rules without any infrastructure dependencies.
    /// No mocks needed - just pure object behavior testing.
    /// Refactored to use TestDataBuilders to eliminate code duplication.
    /// </summary>
    public class OrderTests
    {
        [Fact]
        public void CreateOrder_WithValidCustomerId_ShouldInitializeCorrectly()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            
            // Act - Using domain entity constructor
            var order = new Order(customerId, "test-user");
            
            // Assert
            order.CustomerId.Should().Be(customerId);
            order.Status.Should().Be(OrderStatus.Pending);
            order.Items.Should().NotBeNull().And.BeEmpty();
            order.TotalAmount.Should().Be(0);
            order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AddItem_WithValidProduct_ShouldAddItemCorrectly()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            
            // Act
            order.AddItem(Guid.NewGuid(), "Test Product", 3, 99.99m, "test-user");
            
            // Assert
            order.Items.Should().HaveCount(1);
            var addedItem = order.Items.First();
            addedItem.ProductName.Should().Be("Test Product");
            addedItem.Quantity.Should().Be(3);
            addedItem.UnitPrice.Should().Be(99.99m);
            addedItem.TotalPrice.Should().Be(299.97m); // 3 * 99.99
            
            order.TotalAmount.Should().Be(299.97m);
        }

        [Fact]
        public void AddMultipleItems_ShouldCalculateCorrectTotal()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            
            // Act
            order.AddItem(Guid.NewGuid(), "Standard Product 1", 2, 50.00m, "test-user");
            order.AddItem(Guid.NewGuid(), "Standard Product 2", 1, 75.00m, "test-user");
            
            // Assert
            order.Items.Should().HaveCount(2);
            order.TotalAmount.Should().Be(175.00m); // (2*50) + (1*75)
            
            // Verify individual item totals
            order.Items.First(i => i.ProductName == "Standard Product 1").TotalPrice.Should().Be(100.00m);
            order.Items.First(i => i.ProductName == "Standard Product 2").TotalPrice.Should().Be(75.00m);
        }

        [Fact]
        public void ConfirmOrder_WithValidOrder_ShouldUpdateStatus()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(Guid.NewGuid(), "Test Product", 1, 50.00m, "test-user");
            
            // Act
            order.Confirm("confirming-user");
            
            // Assert
            order.Status.Should().Be(OrderStatus.Confirmed);
        }

        [Fact]
        public void CancelOrder_WithPendingOrder_ShouldUpdateStatus()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(Guid.NewGuid(), "Test Product", 1, 50.00m, "test-user");
            
            // Act
            order.Cancel("cancelling-user", "Customer request");
            
            // Assert
            order.Status.Should().Be(OrderStatus.Cancelled);
        }

        [Fact]
        public void AddItem_WithZeroQuantity_ShouldThrowException()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            
            // Act & Assert
            order.Invoking(o => o.AddItem(Guid.NewGuid(), "Test Product", 0, 99.99m, "test-user"))
                .Should().Throw<ArgumentException>()
                .WithMessage("Quantity must be positive*");
        }

        [Fact]
        public void AddItem_WithNegativePrice_ShouldThrowException()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            
            // Act & Assert
            order.Invoking(o => o.AddItem(Guid.NewGuid(), "Test Product", 1, -10.00m, "test-user"))
                .Should().Throw<ArgumentException>()
                .WithMessage("Unit price cannot be negative*");
        }

        [Fact]
        public void AddItem_WithEmptyProductName_ShouldThrowException()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            
            // Act & Assert
            order.Invoking(o => o.AddItem(Guid.NewGuid(), "", 1, 10.00m, "test-user"))
                .Should().Throw<ArgumentException>()
                .WithMessage("Product name cannot be empty*");
        }

        [Fact]
        public void RemoveItem_FromOrder_ShouldUpdateTotalCorrectly()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(productId, "Product 1", 2, 50.00m, "test-user");
            order.AddItem(Guid.NewGuid(), "Product 2", 1, 75.00m, "test-user");
            var originalTotal = order.TotalAmount;
            
            // Act
            order.RemoveItem(productId, "test-user");
            
            // Assert
            order.Items.Should().HaveCount(1);
            order.TotalAmount.Should().Be(75.00m); // Only Product 2 remains
        }

        [Fact]
        public void UpdateItemQuantity_ShouldRecalculateTotal()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(productId, "Test Product", 2, 50.00m, "test-user");
            var originalTotal = order.TotalAmount; // 100.00
            
            // Act
            order.UpdateItemQuantity(productId, 5, "test-user");
            
            // Assert
            order.TotalAmount.Should().Be(250.00m); // 5 * 50.00
            order.Items.First().Quantity.Should().Be(5);
        }

        [Fact]
        public void Confirm_EmptyOrder_ShouldThrowException()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            
            // Act & Assert
            order.Invoking(o => o.Confirm("test-user"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot confirm an order without items");
        }

        [Fact]
        public void Confirm_AlreadyConfirmedOrder_ShouldThrowException()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(Guid.NewGuid(), "Test Product", 1, 50.00m, "test-user");
            order.Confirm("test-user");
            
            // Act & Assert
            order.Invoking(o => o.Confirm("test-user"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*Only pending orders can be confirmed*");
        }

        [Fact]
        public void AddItem_ToConfirmedOrder_ShouldThrowException()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(Guid.NewGuid(), "Test Product", 1, 50.00m, "test-user");
            order.Confirm("test-user");
            
            // Act & Assert
            order.Invoking(o => o.AddItem(Guid.NewGuid(), "Another Product", 1, 25.00m, "test-user"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot modify order in Confirmed status*");
        }

        [Fact]
        public void MarkAsFulfilled_ConfirmedOrder_ShouldUpdateStatus()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(Guid.NewGuid(), "Test Product", 1, 50.00m, "test-user");
            order.Confirm("test-user");
            
            // Act
            order.MarkAsFulfilled("fulfilling-user");
            
            // Assert
            order.Status.Should().Be(OrderStatus.Fulfilled);
        }

        [Fact]
        public void CanBeModified_PendingOrder_ShouldReturnTrue()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            
            // Act & Assert
            order.CanBeModified().Should().BeTrue();
        }

        [Fact]
        public void CanBeModified_ConfirmedOrder_ShouldReturnFalse()
        {
            // Arrange
            var order = new Order(Guid.NewGuid(), "test-user");
            order.AddItem(Guid.NewGuid(), "Test Product", 1, 50.00m, "test-user");
            order.Confirm("test-user");
            
            // Act & Assert
            order.CanBeModified().Should().BeFalse();
        }
    }
}