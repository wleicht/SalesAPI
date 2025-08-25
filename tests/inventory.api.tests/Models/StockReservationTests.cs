using System;
using FluentAssertions;
using InventoryApi.Models;
using Xunit;

namespace InventoryApi.Tests.Models
{
    /// <summary>
    /// Unit tests for StockReservation business logic and invariants.
    /// Tests core business rules without external dependencies.
    /// </summary>
    public class StockReservationTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Act
            var reservation = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 5
            };

            // Assert
            reservation.Id.Should().NotBeEmpty();
            reservation.Status.Should().Be(ReservationStatus.Reserved);
            reservation.ReservedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            reservation.ProcessedAt.Should().BeNull();
        }

        [Fact]
        public void SetProcessedAt_WhenStatusIsDebited_ShouldSetTimestamp()
        {
            // Arrange
            var reservation = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 5
            };

            var processedTime = DateTime.UtcNow;

            // Act
            reservation.Status = ReservationStatus.Debited;
            reservation.ProcessedAt = processedTime;

            // Assert
            reservation.Status.Should().Be(ReservationStatus.Debited);
            reservation.ProcessedAt.Should().Be(processedTime);
        }

        [Fact]
        public void SetProcessedAt_WhenStatusIsReleased_ShouldSetTimestamp()
        {
            // Arrange
            var reservation = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 5
            };

            var processedTime = DateTime.UtcNow;

            // Act
            reservation.Status = ReservationStatus.Released;
            reservation.ProcessedAt = processedTime;

            // Assert
            reservation.Status.Should().Be(ReservationStatus.Released);
            reservation.ProcessedAt.Should().Be(processedTime);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Quantity_WhenPositive_ShouldBeValid(int quantity)
        {
            // Act
            var reservation = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = quantity
            };

            // Assert
            reservation.Quantity.Should().Be(quantity);
            reservation.Quantity.Should().BePositive();
        }

        [Fact]
        public void CorrelationId_WhenSet_ShouldMaintainValue()
        {
            // Arrange
            var correlationId = "test-correlation-123";
            
            // Act
            var reservation = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 5,
                CorrelationId = correlationId
            };

            // Assert
            reservation.CorrelationId.Should().Be(correlationId);
        }

        [Fact]
        public void ReservationLifecycle_ShouldMaintainConsistentState()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var reservationTime = DateTime.UtcNow;

            // Act - Create reservation
            var reservation = new StockReservation
            {
                OrderId = orderId,
                ProductId = productId,
                ProductName = "Test Product",
                Quantity = 5,
                CorrelationId = "test-123"
            };

            // Assert - Initial state
            reservation.Status.Should().Be(ReservationStatus.Reserved);
            reservation.ProcessedAt.Should().BeNull();
            reservation.ReservedAt.Should().BeCloseTo(reservationTime, TimeSpan.FromSeconds(1));

            // Act - Process reservation (Debit)
            var processedTime = DateTime.UtcNow.AddMinutes(5);
            reservation.Status = ReservationStatus.Debited;
            reservation.ProcessedAt = processedTime;

            // Assert - Final state
            reservation.Status.Should().Be(ReservationStatus.Debited);
            reservation.ProcessedAt.Should().Be(processedTime);
            reservation.OrderId.Should().Be(orderId);
            reservation.ProductId.Should().Be(productId);
        }

        [Fact]
        public void ReservationStatus_ShouldHaveCorrectEnumValues()
        {
            // Assert
            ((int)ReservationStatus.Reserved).Should().Be(1);
            ((int)ReservationStatus.Debited).Should().Be(2);
            ((int)ReservationStatus.Released).Should().Be(3);
        }

        [Fact]
        public void CreateReservation_WithAllRequiredFields_ShouldBeValid()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var productName = "Premium Laptop";
            var quantity = 2;
            var correlationId = "order-processing-456";

            // Act
            var reservation = new StockReservation
            {
                OrderId = orderId,
                ProductId = productId,
                ProductName = productName,
                Quantity = quantity,
                CorrelationId = correlationId
            };

            // Assert
            reservation.Id.Should().NotBeEmpty();
            reservation.OrderId.Should().Be(orderId);
            reservation.ProductId.Should().Be(productId);
            reservation.ProductName.Should().Be(productName);
            reservation.Quantity.Should().Be(quantity);
            reservation.Status.Should().Be(ReservationStatus.Reserved);
            reservation.ReservedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            reservation.ProcessedAt.Should().BeNull();
            reservation.CorrelationId.Should().Be(correlationId);
        }
    }
}