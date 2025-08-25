using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using BuildingBlocks.Contracts.Products;
using BuildingBlocks.Contracts.Orders;
using BuildingBlocks.Events.Domain;
using Xunit;

namespace Contracts.Tests
{
    /// <summary>
    /// Contract tests to validate compatibility between Sales and Inventory APIs.
    /// Ensures DTOs and events maintain consistent structure across services.
    /// </summary>
    public class ContractCompatibilityTests
    {
        [Fact]
        public void ProductDto_ShouldHaveConsistentStructure()
        {
            // Arrange & Act
            var product = new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Description = "Test Description",
                Price = 99.99m,
                StockQuantity = 50,
                CreatedAt = DateTime.UtcNow
            };

            // Assert - Verify all required properties exist
            product.Id.Should().NotBeEmpty();
            product.Name.Should().NotBeNullOrEmpty();
            product.Description.Should().NotBeNullOrEmpty();
            product.Price.Should().BeGreaterOrEqualTo(0);
            product.StockQuantity.Should().BeGreaterOrEqualTo(0);
            product.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void CreateProductDto_ShouldValidateRequiredFields()
        {
            // Arrange & Act
            var createProduct = new CreateProductDto
            {
                Name = "New Product",
                Description = "Product Description",
                Price = 149.99m,
                StockQuantity = 25
            };

            // Assert - Verify structure matches expected interface
            createProduct.Name.Should().NotBeNullOrEmpty();
            createProduct.Description.Should().NotBeNullOrEmpty();
            createProduct.Price.Should().BeGreaterOrEqualTo(0);
            createProduct.StockQuantity.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void OrderDto_ShouldHaveCompatibleStructure()
        {
            // Arrange & Act
            var order = new OrderDto
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed",
                TotalAmount = 299.98m,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        OrderId = Guid.NewGuid(),
                        ProductId = Guid.NewGuid(),
                        ProductName = "Contract Test Product",
                        Quantity = 2,
                        UnitPrice = 149.99m,
                        TotalPrice = 299.98m
                    }
                }
            };

            // Assert - Verify Sales API contract structure
            order.Id.Should().NotBeEmpty();
            order.CustomerId.Should().NotBeEmpty();
            order.Status.Should().NotBeNullOrEmpty();
            order.TotalAmount.Should().BeGreaterOrEqualTo(0);
            order.Items.Should().NotBeNull();
            order.Items.Should().HaveCount(1);
        }

        [Fact]
        public void OrderConfirmedEvent_ShouldMatchExpectedStructure()
        {
            // Arrange & Act
            var orderEvent = new OrderConfirmedEvent
            {
                OrderId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                TotalAmount = 199.99m,
                Items = new List<OrderItemEvent>
                {
                    new OrderItemEvent
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Event Test Product",
                        Quantity = 1,
                        UnitPrice = 199.99m
                    }
                },
                Status = "Confirmed",
                OrderCreatedAt = DateTime.UtcNow,
                CorrelationId = "test-correlation-123"
            };

            // Assert - Verify event contract for cross-service communication
            orderEvent.OrderId.Should().NotBeEmpty();
            orderEvent.CustomerId.Should().NotBeEmpty();
            orderEvent.Items.Should().NotBeNull();
            orderEvent.Items.Should().HaveCount(1);
            orderEvent.CorrelationId.Should().NotBeNullOrEmpty();
            
            // Verify event inherits from DomainEvent
            orderEvent.EventId.Should().NotBeEmpty();
            orderEvent.OccurredAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void OrderCancelledEvent_ShouldHaveCompensationFields()
        {
            // Arrange & Act
            var cancelEvent = new OrderCancelledEvent
            {
                OrderId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                TotalAmount = 99.99m,
                Items = new List<OrderItemEvent>
                {
                    new OrderItemEvent
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Cancelled Product",
                        Quantity = 1,
                        UnitPrice = 99.99m
                    }
                },
                CancellationReason = "Payment failed",
                Status = "Cancelled",
                OrderCreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CancelledAt = DateTime.UtcNow,
                CorrelationId = "cancel-correlation-456"
            };

            // Assert - Verify cancellation event structure for compensation
            cancelEvent.OrderId.Should().NotBeEmpty();
            cancelEvent.CancellationReason.Should().NotBeNullOrEmpty();
            cancelEvent.CancelledAt.Should().BeAfter(DateTime.MinValue);
            cancelEvent.Status.Should().Be("Cancelled");
            cancelEvent.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void StockDebitedEvent_ShouldProvideInventoryFeedback()
        {
            // Arrange & Act
            var stockEvent = new StockDebitedEvent
            {
                OrderId = Guid.NewGuid(),
                StockDeductions = new List<StockDeduction>
                {
                    new StockDeduction
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Stock Test Product",
                        QuantityDebited = 2,
                        PreviousStock = 100,
                        NewStock = 98
                    }
                },
                AllDeductionsSuccessful = true,
                ErrorMessage = null,
                CorrelationId = "stock-correlation-789"
            };

            // Assert - Verify inventory feedback structure
            stockEvent.OrderId.Should().NotBeEmpty();
            stockEvent.StockDeductions.Should().NotBeNull();
            stockEvent.StockDeductions.Should().HaveCount(1);
            stockEvent.AllDeductionsSuccessful.Should().BeTrue();
            stockEvent.ErrorMessage.Should().BeNull();
            stockEvent.CorrelationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void DomainEvent_BaseProperties_ShouldBeConsistent()
        {
            // Arrange & Act
            var domainEvent = new OrderConfirmedEvent
            {
                OrderId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                TotalAmount = 100.00m,
                Items = new List<OrderItemEvent>
                {
                    new OrderItemEvent
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        Quantity = 1,
                        UnitPrice = 100.00m
                    }
                },
                Status = "Confirmed",
                OrderCreatedAt = DateTime.UtcNow,
                CorrelationId = "base-test-123"
            };

            // Assert - Verify base event properties
            domainEvent.EventId.Should().NotBeEmpty();
            domainEvent.OccurredAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
            domainEvent.CorrelationId.Should().Be("base-test-123");
        }

        [Fact]
        public void CrossServiceDataFlow_ShouldMaintainConsistency()
        {
            // Arrange - Simulate cross-service data flow
            var productId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var correlationId = "cross-service-test";

            // Act - Create objects that would flow between services
            var product = new ProductDto
            {
                Id = productId,
                Name = "Cross-Service Product",
                Price = 100.00m,
                StockQuantity = 50
            };

            var orderItem = new OrderItemDto
            {
                OrderId = orderId,
                ProductId = productId,
                ProductName = product.Name,
                Quantity = 2,
                UnitPrice = product.Price,
                TotalPrice = 200.00m
            };

            var orderEvent = new OrderConfirmedEvent
            {
                OrderId = orderId,
                CustomerId = customerId,
                TotalAmount = 200.00m,
                Items = new List<OrderItemEvent>
                {
                    new OrderItemEvent
                    {
                        ProductId = productId,
                        ProductName = product.Name,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice
                    }
                },
                Status = "Confirmed",
                OrderCreatedAt = DateTime.UtcNow,
                CorrelationId = correlationId
            };

            // Assert - Verify data consistency across service boundaries
            orderItem.ProductId.Should().Be(product.Id);
            orderItem.ProductName.Should().Be(product.Name);
            orderItem.UnitPrice.Should().Be(product.Price);
            
            orderEvent.Items.First().ProductId.Should().Be(product.Id);
            orderEvent.Items.First().ProductName.Should().Be(product.Name);
            orderEvent.Items.First().Quantity.Should().Be(orderItem.Quantity);
            orderEvent.CorrelationId.Should().Be(correlationId);
        }

        [Fact]
        public void EventSequence_ShouldMaintainOrderIntegrity()
        {
            // Arrange - Simulate event sequence
            var orderId = Guid.NewGuid();
            var baseTime = DateTime.UtcNow;

            // Act - Create event sequence
            var orderConfirmed = new OrderConfirmedEvent
            {
                OrderId = orderId,
                CustomerId = Guid.NewGuid(),
                TotalAmount = 100.00m,
                Items = new List<OrderItemEvent>
                {
                    new OrderItemEvent
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Sequence Test Product",
                        Quantity = 1,
                        UnitPrice = 100.00m
                    }
                },
                Status = "Confirmed",
                OrderCreatedAt = baseTime,
                CorrelationId = "sequence-test"
            };

            var stockDebited = new StockDebitedEvent
            {
                OrderId = orderId,
                StockDeductions = new List<StockDeduction>
                {
                    new StockDeduction
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        QuantityDebited = 1,
                        PreviousStock = 10,
                        NewStock = 9
                    }
                },
                AllDeductionsSuccessful = true,
                CorrelationId = "sequence-test"
            };

            // Assert - Verify event sequence integrity
            orderConfirmed.OrderId.Should().Be(stockDebited.OrderId);
            orderConfirmed.CorrelationId.Should().Be(stockDebited.CorrelationId);
            orderConfirmed.OccurredAt.Should().BeBefore(stockDebited.OccurredAt);
        }
    }
}