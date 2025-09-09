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
using SalesApi.Application.Commands;
using SalesApi.Domain.Services;
using SalesApi.Domain.Repositories;
using SalesApi.Domain.Entities;
using SalesApi.Application.DTOs;

namespace SalesApi.Tests.Application.Handlers
{
    /// <summary>
    /// Unit tests for CreateOrderCommandHandler demonstrating the improved testability
    /// achieved through CQRS pattern and MediatR implementation.
    /// </summary>
    /// <remarks>
    /// Testing Benefits from Refactoring:
    /// 
    /// Isolation and Mocking:
    /// - Command handlers are isolated and easily mockable
    /// - Clear dependencies make unit testing straightforward
    /// - No web framework dependencies in business logic tests
    /// - Pure business logic testing without HTTP concerns
    /// 
    /// Test Coverage Improvement:
    /// - Each command handler can be tested independently
    /// - Business logic is separated from presentation logic
    /// - Error scenarios can be tested in isolation
    /// - Validation logic is testable without HTTP context
    /// 
    /// Maintenance Benefits:
    /// - Tests are more focused and maintainable
    /// - Changes in web layer don't break business logic tests
    /// - Domain service interactions are clearly tested
    /// - Repository interactions are properly mocked
    /// </remarks>
    public class CreateOrderCommandHandlerTests
    {
        private readonly Mock<IOrderDomainService> _mockDomainService;
        private readonly Mock<IOrderRepository> _mockRepository;
        private readonly Mock<ILogger<CreateOrderCommandHandler>> _mockLogger;
        private readonly CreateOrderCommandHandler _handler;

        public CreateOrderCommandHandlerTests()
        {
            _mockDomainService = new Mock<IOrderDomainService>();
            _mockRepository = new Mock<IOrderRepository>();
            _mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();
            
            _handler = new CreateOrderCommandHandler(
                _mockDomainService.Object,
                _mockRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessResult()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemCommand>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 2, ProductName = "Test Product", UnitPrice = 10.00m }
                },
                CreatedBy = "test-user",
                CorrelationId = "test-correlation"
            };

            var expectedOrder = CreateTestOrder(Guid.NewGuid(), command.CustomerId, "Confirmed", 20.00m);
            AddTestItem(expectedOrder, expectedOrder.Id, command.Items[0].ProductId, "Test Product", 2, 10.00m);

            _mockDomainService
                .Setup(x => x.CreateOrderAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IEnumerable<CreateOrderItemRequest>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedOrder);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Order);
            Assert.Equal(expectedOrder.Id, result.Order.Id);
            Assert.Equal(expectedOrder.TotalAmount, result.Order.TotalAmount);

            // Verify domain service was called with correct parameters
            _mockDomainService.Verify(x => x.CreateOrderAsync(
                command.CustomerId,
                It.Is<IEnumerable<CreateOrderItemRequest>>(items => 
                    items.Count() == 1 && 
                    items.First().ProductId == command.Items[0].ProductId),
                command.CreatedBy,
                command.CorrelationId,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmptyItems_ReturnsValidationFailure()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemCommand>(), // Empty items
                CreatedBy = "test-user"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
            Assert.Contains("Order must contain at least one item", result.ValidationErrors);

            // Verify domain service was not called
            _mockDomainService.Verify(x => x.CreateOrderAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<CreateOrderItemRequest>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DomainServiceThrowsArgumentException_ReturnsFailureResult()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemCommand>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, ProductName = "Test", UnitPrice = 10 }
                },
                CreatedBy = "test-user"
            };

            _mockDomainService
                .Setup(x => x.CreateOrderAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IEnumerable<CreateOrderItemRequest>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid customer"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("INVALID_ARGUMENT", result.ErrorCode);
            Assert.Equal("Invalid customer", result.ErrorMessage);
        }

        [Fact]
        public async Task Handle_DomainServiceThrowsInvalidOperationException_ReturnsBusinessRuleViolation()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemCommand>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, ProductName = "Test", UnitPrice = 10 }
                },
                CreatedBy = "test-user"
            };

            _mockDomainService
                .Setup(x => x.CreateOrderAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IEnumerable<CreateOrderItemRequest>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Insufficient stock"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("BUSINESS_RULE_VIOLATION", result.ErrorCode);
            Assert.Equal("Insufficient stock", result.ErrorMessage);
        }

        [Fact]
        public async Task Handle_InvalidCustomerId_ReturnsValidationFailure()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.Empty, // Invalid customer ID
                Items = new List<CreateOrderItemCommand>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, ProductName = "Test", UnitPrice = 10 }
                },
                CreatedBy = "test-user"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
            Assert.Contains("Customer ID is required", result.ValidationErrors);
        }

        [Fact]
        public async Task Handle_NullOrEmptyCreatedBy_ReturnsValidationFailure()
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemCommand>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, ProductName = "Test", UnitPrice = 10 }
                },
                CreatedBy = string.Empty // Empty created by
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
            Assert.Contains("CreatedBy is required", result.ValidationErrors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task Handle_InvalidQuantity_ReturnsValidationFailure(int invalidQuantity)
        {
            // Arrange
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemCommand>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = invalidQuantity, ProductName = "Test", UnitPrice = 10 }
                },
                CreatedBy = "test-user"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
            Assert.Contains("Quantity must be positive for product", result.ValidationErrors.First());
        }

        [Fact]
        public async Task Handle_DuplicateProducts_ReturnsValidationFailure()
        {
            // Arrange
            var duplicateProductId = Guid.NewGuid();
            var command = new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemCommand>
                {
                    new() { ProductId = duplicateProductId, Quantity = 1, ProductName = "Test1", UnitPrice = 10 },
                    new() { ProductId = duplicateProductId, Quantity = 2, ProductName = "Test2", UnitPrice = 15 } // Duplicate
                },
                CreatedBy = "test-user"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
            Assert.Contains($"Duplicate product found: {duplicateProductId}", result.ValidationErrors);
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