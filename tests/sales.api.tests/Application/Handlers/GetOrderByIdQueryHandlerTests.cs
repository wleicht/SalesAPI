using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using SalesApi.Application.Handlers;
using SalesApi.Application.Queries;
using SalesApi.Domain.Repositories;
using SalesApi.Domain.Entities;
using SalesApi.Application.DTOs;

namespace SalesApi.Tests.Application.Handlers
{
    /// <summary>
    /// Unit tests for GetOrderByIdQueryHandler demonstrating the improved testability
    /// achieved through CQRS query pattern and repository abstraction.
    /// </summary>
    /// <remarks>
    /// Testing Benefits from Repository Pattern:
    /// 
    /// Data Access Abstraction:
    /// - Repository interface allows easy mocking of data layer
    /// - Query handlers can be tested without database dependencies
    /// - Different data scenarios can be easily simulated
    /// - Performance testing is isolated from actual database calls
    /// 
    /// Query Logic Testing:
    /// - Data mapping logic can be thoroughly tested
    /// - Error handling scenarios are easily testable
    /// - Edge cases can be simulated through mocked data
    /// - Business logic in queries is independently verifiable
    /// 
    /// Maintenance Benefits:
    /// - Changes in database schema don't break query logic tests
    /// - Query optimization can be tested without infrastructure
    /// - Data transformation logic is clearly testable
    /// - Repository contract compliance is verifiable
    /// </remarks>
    public class GetOrderByIdQueryHandlerTests
    {
        private readonly Mock<IOrderRepository> _mockRepository;
        private readonly Mock<ILogger<GetOrderByIdQueryHandler>> _mockLogger;
        private readonly GetOrderByIdQueryHandler _handler;

        public GetOrderByIdQueryHandlerTests()
        {
            _mockRepository = new Mock<IOrderRepository>();
            _mockLogger = new Mock<ILogger<GetOrderByIdQueryHandler>>();
            
            _handler = new GetOrderByIdQueryHandler(
                _mockRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ExistingOrderWithItems_ReturnsOrderDto()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            
            var order = CreateTestOrder(orderId, customerId, "Confirmed", 100.00m);
            AddTestItem(order, orderId, productId, "Test Product", 2, 50.00m);

            var query = new GetOrderByIdQuery { OrderId = orderId, IncludeItems = true };

            _mockRepository
                .Setup(x => x.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal(customerId, result.CustomerId);
            Assert.Equal("Confirmed", result.Status);
            Assert.Single(result.Items);
            
            var item = result.Items.First();
            Assert.Equal(productId, item.ProductId);
            Assert.Equal("Test Product", item.ProductName);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(50.00m, item.UnitPrice);

            // Verify correct repository method was called
            _mockRepository.Verify(x => x.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ExistingOrderWithoutItemsRequested_ReturnsOrderDtoWithoutItems()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            
            var order = CreateTestOrder(orderId, customerId, "Pending", 50.00m);

            var query = new GetOrderByIdQuery { OrderId = orderId, IncludeItems = false };

            _mockRepository
                .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderId, result.Id);
            Assert.Equal(customerId, result.CustomerId);
            Assert.Equal("Pending", result.Status);
            Assert.Equal(50.00m, result.TotalAmount);

            // Verify correct repository method was called
            _mockRepository.Verify(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(x => x.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NonExistentOrder_ReturnsNull()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var query = new GetOrderByIdQuery { OrderId = orderId, IncludeItems = true };

            _mockRepository
                .Setup(x => x.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(x => x.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_RepositoryThrowsException_ThrowsException()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var query = new GetOrderByIdQuery { OrderId = orderId, IncludeItems = true };

            _mockRepository
                .Setup(x => x.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database connection error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(query, CancellationToken.None));
            
            Assert.Equal("Database connection error", exception.Message);
            _mockRepository.Verify(x => x.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_OrderWithMultipleItems_CalculatesTotalCorrectly()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            
            var order = CreateTestOrder(orderId, customerId, "Confirmed", 175.00m);
            AddTestItem(order, orderId, Guid.NewGuid(), "Product A", 2, 50.00m);
            AddTestItem(order, orderId, Guid.NewGuid(), "Product B", 3, 25.00m);

            var query = new GetOrderByIdQuery { OrderId = orderId, IncludeItems = true };

            _mockRepository
                .Setup(x => x.GetWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);

            // Verify individual item calculations
            var itemA = result.Items.First(i => i.ProductName == "Product A");
            Assert.Equal(100.00m, itemA.TotalPrice);

            var itemB = result.Items.First(i => i.ProductName == "Product B");
            Assert.Equal(75.00m, itemB.TotalPrice);
        }

        [Fact]
        public async Task Handle_OrderWithAuditFields_MapsAuditInformation()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            
            var order = CreateTestOrder(orderId, customerId, "Fulfilled", 200.00m, null, null, "customer-123", null);

            var query = new GetOrderByIdQuery { OrderId = orderId, IncludeItems = false };

            _mockRepository
                .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("customer-123", result.CreatedBy);
            Assert.True(result.CreatedAt != default(DateTime));
            // The UpdatedBy should default to CreatedBy when no updates have occurred
            Assert.Equal("customer-123", result.UpdatedBy);
        }

        #region Helper Methods

        /// <summary>
        /// Creates a test Order with specified properties using reflection to set readonly properties.
        /// </summary>
        private static Order CreateTestOrder(Guid orderId, Guid customerId, string status, decimal totalAmount,
            DateTime? createdAt = null, DateTime? updatedAt = null, string? createdBy = null, string? updatedBy = null)
        {
            var order = new Order(customerId, createdBy ?? "test-user");

            // Use reflection to set readonly properties
            SetPrivateProperty(order, "Id", orderId);
            SetPrivateProperty(order, "Status", status);
            SetPrivateProperty(order, "TotalAmount", totalAmount);
            
            if (createdAt.HasValue)
                SetPrivateProperty(order, "CreatedAt", createdAt.Value);
            
            if (updatedAt.HasValue)
                SetPrivateProperty(order, "UpdatedAt", updatedAt.Value);
            
            if (!string.IsNullOrEmpty(updatedBy))
                SetPrivateProperty(order, "UpdatedBy", updatedBy);

            return order;
        }

        /// <summary>
        /// Adds a test OrderItem to an Order using reflection to access private collections.
        /// </summary>
        private static void AddTestItem(Order order, Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            var orderItem = new OrderItem(orderId, productId, productName, quantity, unitPrice);
            
            // Get the Items collection and add the item directly
            var itemsProperty = typeof(Order).GetProperty("Items");
            if (itemsProperty?.GetValue(order) is ICollection<OrderItem> items)
            {
                items.Add(orderItem);
            }
        }

        /// <summary>
        /// Sets a private property value using reflection.
        /// </summary>
        private static void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            else
            {
                // Try to find backing field if property is readonly
                var field = obj.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
        }

        #endregion
    }
}