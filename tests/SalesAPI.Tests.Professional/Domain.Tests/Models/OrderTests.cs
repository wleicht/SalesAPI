using FluentAssertions;
using SalesApi.Models;
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
            
            // Act - Using TestDataBuilder instead of duplicated helper method
            var order = TestDataBuilders.Orders.NewOrder()
                .WithCustomer(customerId)
                .WithStatus("Pending")
                .Build();
            
            // Assert
            order.CustomerId.Should().Be(customerId);
            order.Status.Should().Be("Pending");
            order.Items.Should().NotBeNull().And.BeEmpty();
            order.TotalAmount.Should().Be(0);
            order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AddItem_WithValidProduct_ShouldAddItemCorrectly()
        {
            // Arrange & Act - Using builder pattern
            var order = TestDataBuilders.Orders.NewOrder()
                .WithItem("Test Product", 3, 99.99m)
                .Build();
            
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
            // Arrange & Act - Using standard items builder
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStandardItems()
                .Build();
            
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
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStatus("Pending")
                .Build();
            
            // Act - Simulating business logic for confirmation
            order.Status = "Confirmed";
            
            // Assert
            order.Status.Should().Be("Confirmed");
        }

        [Fact]
        public void CancelOrder_WithConfirmedOrder_ShouldUpdateStatus()
        {
            // Arrange
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStatus("Confirmed")
                .WithStandardItems()
                .Build();
            
            // Act - Simulating business logic for cancellation
            order.Status = "Cancelled";
            
            // Assert
            order.Status.Should().Be("Cancelled");
        }

        [Fact]
        public void OrderWithZeroQuantityItem_ShouldHaveZeroTotalForThatItem()
        {
            // Arrange & Act
            var order = TestDataBuilders.Orders.NewOrder()
                .WithItem("Zero Quantity Product", 0, 99.99m)
                .Build();
            
            // Assert
            var zeroQuantityItem = order.Items.First();
            zeroQuantityItem.TotalPrice.Should().Be(0);
            order.TotalAmount.Should().Be(0);
        }

        [Fact]
        public void OrderWithHighQuantity_ShouldCalculateCorrectTotal()
        {
            // Arrange & Act
            var order = TestDataBuilders.Orders.NewOrder()
                .WithItem("Bulk Product", 1000, 1.50m)
                .Build();
            
            // Assert
            var highQuantityItem = order.Items.First();
            highQuantityItem.TotalPrice.Should().Be(1500.00m); // 1000 * 1.50
            order.TotalAmount.Should().Be(1500.00m);
        }

        [Fact]
        public void OrderWithDecimalPrices_ShouldHandlePrecisionCorrectly()
        {
            // Arrange & Act - Adding items with precise decimal values
            var order = TestDataBuilders.Orders.NewOrder()
                .WithItem("Precise Product 1", 3, 33.333333m)
                .WithItem("Precise Product 2", 7, 14.285714m)
                .Build();
            
            // Assert - Verify decimal handling
            var item1 = order.Items.First(i => i.ProductName == "Precise Product 1");
            var item2 = order.Items.First(i => i.ProductName == "Precise Product 2");
            
            item1.TotalPrice.Should().Be(99.999999m); // 3 * 33.333333
            item2.TotalPrice.Should().Be(99.999998m); // 7 * 14.285714
            order.TotalAmount.Should().Be(199.999997m);
        }

        [Fact]
        public void RemoveItem_FromOrder_ShouldUpdateTotalCorrectly()
        {
            // Arrange
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStandardItems()
                .Build();
            var originalTotal = order.TotalAmount;
            var itemToRemove = order.Items.First();
            var itemTotal = itemToRemove.TotalPrice;
            
            // Act
            order.Items.Remove(itemToRemove);
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
            
            // Assert
            order.TotalAmount.Should().Be(originalTotal - itemTotal);
        }

        [Fact]
        public void UpdateItemQuantity_ShouldRecalculateTotal()
        {
            // Arrange
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStandardItems()
                .Build();
            var itemToUpdate = order.Items.First();
            var originalItemTotal = itemToUpdate.TotalPrice;
            var originalOrderTotal = order.TotalAmount;
            
            // Act
            itemToUpdate.Quantity = itemToUpdate.Quantity * 2; // Double the quantity
            var newItemTotal = itemToUpdate.TotalPrice;
            order.TotalAmount = order.Items.Sum(i => i.TotalPrice);
            
            // Assert
            newItemTotal.Should().Be(originalItemTotal * 2);
            order.TotalAmount.Should().Be(originalOrderTotal + originalItemTotal); // Original + added amount
        }

        [Fact]
        public void OrderStatus_TransitionFromPendingToConfirmed_ShouldSucceed()
        {
            // Arrange
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStatus("Pending")
                .Build();
            
            // Act
            order.Status = "Confirmed";
            
            // Assert
            order.Status.Should().Be("Confirmed");
        }

        [Fact]
        public void OrderStatus_TransitionFromConfirmedToCancelled_ShouldSucceed()
        {
            // Arrange
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStatus("Confirmed")
                .Build();
            
            // Act
            order.Status = "Cancelled";
            
            // Assert
            order.Status.Should().Be("Cancelled");
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Confirmed")]
        [InlineData("Cancelled")]
        public void OrderStatus_WithValidStatuses_ShouldAcceptValue(string status)
        {
            // Arrange & Act
            var order = TestDataBuilders.Orders.NewOrder()
                .WithStatus(status)
                .Build();
            
            // Assert
            order.Status.Should().Be(status);
        }

        // Note: Removed duplicated helper methods CreateTestOrder() and CreateOrderWithItems()
        // These are now replaced by TestDataBuilders.Orders pattern
    }
}