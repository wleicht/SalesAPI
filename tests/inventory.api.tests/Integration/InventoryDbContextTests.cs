using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using InventoryApi.Models;
using InventoryApi.Persistence;
using Xunit;

namespace InventoryApi.Tests.Integration
{
    /// <summary>
    /// Integration tests for InventoryDbContext with in-memory database.
    /// Tests repository patterns and database operations without external dependencies.
    /// </summary>
    public class InventoryDbContextTests : IDisposable
    {
        private readonly InventoryDbContext _context;

        public InventoryDbContextTests()
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new InventoryDbContext(options);
        }

        [Fact]
        public async Task AddProduct_ShouldPersistToDatabase()
        {
            // Arrange
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Description = "Test Description",
                Price = 99.99m,
                StockQuantity = 50
            };

            // Act
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Assert
            var savedProduct = await _context.Products.FindAsync(product.Id);
            savedProduct.Should().NotBeNull();
            savedProduct!.Name.Should().Be("Test Product");
            savedProduct.Price.Should().Be(99.99m);
            savedProduct.StockQuantity.Should().Be(50);
        }

        [Fact]
        public async Task UpdateProductStock_ShouldModifyQuantity()
        {
            // Arrange
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Stock Test Product",
                Description = "For stock testing",
                Price = 50.00m,
                StockQuantity = 100
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            product.StockQuantity = 95; // Simulate stock deduction
            await _context.SaveChangesAsync();

            // Assert
            var updatedProduct = await _context.Products.FindAsync(product.Id);
            updatedProduct!.StockQuantity.Should().Be(95);
        }

        [Fact]
        public async Task AddStockReservation_ShouldCreateReservationRecord()
        {
            // Arrange
            var reservation = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Reserved Product",
                Quantity = 5,
                CorrelationId = "test-correlation"
            };

            // Act
            _context.StockReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Assert
            var savedReservation = await _context.StockReservations.FindAsync(reservation.Id);
            savedReservation.Should().NotBeNull();
            savedReservation!.Status.Should().Be(ReservationStatus.Reserved);
            savedReservation.Quantity.Should().Be(5);
            savedReservation.CorrelationId.Should().Be("test-correlation");
        }

        [Fact]
        public async Task QueryReservationsByOrder_ShouldReturnCorrectResults()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var reservations = new List<StockReservation>
            {
                new StockReservation
                {
                    OrderId = orderId,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product 1",
                    Quantity = 2
                },
                new StockReservation
                {
                    OrderId = orderId,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product 2",
                    Quantity = 3
                },
                new StockReservation
                {
                    OrderId = Guid.NewGuid(), // Different order
                    ProductId = Guid.NewGuid(),
                    ProductName = "Product 3",
                    Quantity = 1
                }
            };

            _context.StockReservations.AddRange(reservations);
            await _context.SaveChangesAsync();

            // Act
            var orderReservations = await _context.StockReservations
                .Where(r => r.OrderId == orderId)
                .ToListAsync();

            // Assert
            orderReservations.Should().HaveCount(2);
            orderReservations.All(r => r.OrderId == orderId).Should().BeTrue();
            orderReservations.Sum(r => r.Quantity).Should().Be(5);
        }

        [Fact]
        public async Task UpdateReservationStatus_ShouldModifyStatus()
        {
            // Arrange
            var reservation = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Status Test Product",
                Quantity = 3
            };

            _context.StockReservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Act
            reservation.Status = ReservationStatus.Debited;
            reservation.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Assert
            var updatedReservation = await _context.StockReservations.FindAsync(reservation.Id);
            updatedReservation!.Status.Should().Be(ReservationStatus.Debited);
            updatedReservation.ProcessedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ConcurrentStockUpdate_ShouldMaintainConsistency()
        {
            // Arrange
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Concurrent Test Product",
                Description = "For concurrency testing",
                Price = 25.00m,
                StockQuantity = 10
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act - Simulate concurrent stock reservations
            var reservation1 = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 5
            };

            var reservation2 = new StockReservation
            {
                OrderId = Guid.NewGuid(),
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = 3
            };

            _context.StockReservations.AddRange(reservation1, reservation2);
            await _context.SaveChangesAsync();

            // Assert
            var productReservations = await _context.StockReservations
                .Where(r => r.ProductId == product.Id)
                .ToListAsync();

            productReservations.Should().HaveCount(2);
            productReservations.Sum(r => r.Quantity).Should().Be(8);
            productReservations.All(r => r.Status == ReservationStatus.Reserved).Should().BeTrue();
        }

        [Fact]
        public async Task ProcessedEvents_ShouldTrackEventProcessing()
        {
            // Arrange
            var processedEvent = new ProcessedEvent
            {
                EventId = Guid.NewGuid(),
                EventType = "OrderConfirmedEvent",
                OrderId = Guid.NewGuid(),
                ProcessedAt = DateTime.UtcNow,
                CorrelationId = "event-processing-test",
                ProcessingDetails = "Test event processing"
            };

            // Act
            _context.ProcessedEvents.Add(processedEvent);
            await _context.SaveChangesAsync();

            // Assert
            var savedEvent = await _context.ProcessedEvents
                .FirstOrDefaultAsync(pe => pe.EventId == processedEvent.EventId);

            savedEvent.Should().NotBeNull();
            savedEvent!.EventType.Should().Be("OrderConfirmedEvent");
            savedEvent.CorrelationId.Should().Be("event-processing-test");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}