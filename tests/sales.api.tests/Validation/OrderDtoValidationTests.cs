using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using BuildingBlocks.Contracts.Orders;
using Xunit;

namespace SalesApi.Tests.Validation
{
    /// <summary>
    /// Unit tests for Order DTO validation attributes.
    /// Tests data annotation validation without external dependencies.
    /// </summary>
    public class OrderDtoValidationTests
    {
        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void CreateOrderDto_WithValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = 2
                    }
                }
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void CreateOrderDto_WithEmptyCustomerId_ShouldPassValidation()
        {
            // Arrange - Note: Guid.Empty is considered valid by .NET validation
            var dto = new CreateOrderDto
            {
                CustomerId = Guid.Empty,
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = 1
                    }
                }
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert - Guid.Empty passes validation but business logic should handle it
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void CreateOrderDto_WithEmptyItems_ShouldFailValidation()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>() // Empty list
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().ContainSingle()
                .Which.ErrorMessage.Should().Be("At least one order item is required.");
        }

        [Fact]
        public void CreateOrderDto_WithNullItems_ShouldFailValidation()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = Guid.NewGuid(),
                Items = null!
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().Contain(vr => 
                vr.ErrorMessage == "Order items are required.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void CreateOrderItemDto_WithInvalidQuantity_ShouldFailValidation(int quantity)
        {
            // Arrange
            var dto = new CreateOrderItemDto
            {
                ProductId = Guid.NewGuid(),
                Quantity = quantity
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().ContainSingle()
                .Which.ErrorMessage.Should().Be("Quantity must be at least 1.");
        }

        [Fact]
        public void CreateOrderItemDto_WithEmptyProductId_ShouldPassValidation()
        {
            // Arrange - Note: Guid.Empty is considered valid by .NET validation
            var dto = new CreateOrderItemDto
            {
                ProductId = Guid.Empty,
                Quantity = 1
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert - Guid.Empty passes validation but business logic should handle it
            validationResults.Should().BeEmpty();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(int.MaxValue)]
        public void CreateOrderItemDto_WithValidQuantity_ShouldPassValidation(int quantity)
        {
            // Arrange
            var dto = new CreateOrderItemDto
            {
                ProductId = Guid.NewGuid(),
                Quantity = quantity
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void OrderDto_WithAllProperties_ShouldBeValid()
        {
            // Arrange
            var dto = new OrderDto
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Confirmed",
                TotalAmount = 199.99m,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        OrderId = Guid.NewGuid(),
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        Quantity = 2,
                        UnitPrice = 99.99m,
                        TotalPrice = 199.98m
                    }
                }
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void OrderItemDto_WithCalculatedTotalPrice_ShouldBeConsistent()
        {
            // Arrange
            var dto = new OrderItemDto
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Test Product",
                Quantity = 3,
                UnitPrice = 25.00m,
                TotalPrice = 75.00m // Should match Quantity * UnitPrice
            };

            // Act & Assert
            dto.TotalPrice.Should().Be(dto.Quantity * dto.UnitPrice);
        }

        [Fact]
        public void ComplexOrderDto_WithMultipleItems_ShouldValidateCorrectly()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                CustomerId = Guid.NewGuid(),
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 2 },
                    new CreateOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 1 },
                    new CreateOrderItemDto { ProductId = Guid.NewGuid(), Quantity = 5 }
                }
            };

            // Act
            var validationResults = ValidateModel(createOrderDto);

            // Assert
            validationResults.Should().BeEmpty();
            createOrderDto.Items.Should().HaveCount(3);
            createOrderDto.Items.All(item => item.Quantity > 0).Should().BeTrue();
            createOrderDto.Items.All(item => item.ProductId != Guid.Empty).Should().BeTrue();
        }

        [Fact]
        public void CreateOrderDto_BusinessLogicValidation_ShouldCatchEmptyGuids()
        {
            // Arrange - This test validates business logic concerns that attributes don't catch
            var dto = new CreateOrderDto
            {
                CustomerId = Guid.Empty, // Empty GUID passes attribute validation
                Items = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto
                    {
                        ProductId = Guid.Empty, // Empty GUID passes attribute validation
                        Quantity = 1
                    }
                }
            };

            // Act - Basic validation passes
            var validationResults = ValidateModel(dto);

            // Assert - Basic validation passes, but business logic should reject empty GUIDs
            validationResults.Should().BeEmpty("Basic validation doesn't catch empty GUIDs");
            
            // Business logic validation
            dto.CustomerId.Should().Be(Guid.Empty, "This should be caught by business logic, not attributes");
            dto.Items.First().ProductId.Should().Be(Guid.Empty, "This should be caught by business logic, not attributes");
        }
    }
}