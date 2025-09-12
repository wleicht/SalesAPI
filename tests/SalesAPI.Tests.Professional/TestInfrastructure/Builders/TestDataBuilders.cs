using SalesApi.Models;
using InventoryApi.Models;

namespace SalesAPI.Tests.Professional.TestInfrastructure.Builders
{
    /// <summary>
    /// Consolidated test data builders to eliminate duplication across test files
    /// </summary>
    public static class TestDataBuilders
    {
        public static class Orders
        {
            public static OrderBuilder NewOrder() => new OrderBuilder();
        }

        public static class Products  
        {
            public static ProductBuilder NewProduct() => new ProductBuilder();
        }
    }

    public class OrderBuilder
    {
        private Order _order;

        public OrderBuilder()
        {
            _order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };
        }

        public OrderBuilder WithId(Guid id)
        {
            _order.Id = id;
            return this;
        }

        public OrderBuilder WithCustomer(Guid customerId)
        {
            _order.CustomerId = customerId;
            return this;
        }

        public OrderBuilder WithStatus(string status)
        {
            _order.Status = status;
            return this;
        }

        public OrderBuilder WithItems(params OrderItem[] items)
        {
            _order.Items = items.ToList();
            _order.TotalAmount = _order.Items.Sum(i => i.TotalPrice);
            return this;
        }

        public OrderBuilder WithStandardItems()
        {
            var items = new[]
            {
                new OrderItem
                {
                    OrderId = _order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Standard Product 1",
                    Quantity = 2,
                    UnitPrice = 50.00m
                },
                new OrderItem
                {
                    OrderId = _order.Id,
                    ProductId = Guid.NewGuid(),
                    ProductName = "Standard Product 2",
                    Quantity = 1,
                    UnitPrice = 75.00m
                }
            };

            return WithItems(items);
        }

        public OrderBuilder WithItem(string productName, int quantity, decimal unitPrice)
        {
            var item = new OrderItem
            {
                OrderId = _order.Id,
                ProductId = Guid.NewGuid(),
                ProductName = productName,
                Quantity = quantity,
                UnitPrice = unitPrice
            };

            _order.Items.Add(item);
            return this;
        }

        public Order Build()
        {
            _order.TotalAmount = _order.Items.Sum(i => i.TotalPrice);
            return _order;
        }
    }

    public class ProductBuilder
    {
        private Product _product;

        public ProductBuilder()
        {
            _product = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Test Product",
                Description = "A test product",
                Price = 99.99m,
                StockQuantity = 50
            };
        }

        public ProductBuilder WithId(Guid id)
        {
            _product.Id = id;
            return this;
        }

        public ProductBuilder WithName(string name)
        {
            _product.Name = name;
            return this;
        }

        public ProductBuilder WithPrice(decimal price)
        {
            _product.Price = price;
            return this;
        }

        public ProductBuilder WithStock(int quantity)
        {
            _product.StockQuantity = quantity;
            return this;
        }

        public ProductBuilder WithDescription(string description)
        {
            _product.Description = description;
            return this;
        }

        public Product Build() => _product;
    }
}