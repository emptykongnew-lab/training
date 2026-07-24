using OrderHub.Core.Domain;

namespace OrderHub.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsAllProductsIncludingInactive()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetActive_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001");
        TestSetup.AddProduct(db, sku: "SKU-A002", isActive: false);

        var products = await service.GetActiveAsync();

        Assert.All(products, p => Assert.True(p.IsActive));
        Assert.Single(products);
    }

    [Fact]
    public async Task GetLowStock_FiltersByThresholdAndOrdersByStockAscending()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001", stock: 3);
        TestSetup.AddProduct(db, sku: "SKU-A002", stock: 8);
        TestSetup.AddProduct(db, sku: "SKU-A003", stock: 15);

        var results = await service.GetLowStockAsync(10);

        Assert.Equal(new[] { 3, 8 }, results.Select(r => r.Product.StockQuantity));
    }

    [Fact]
    public async Task GetLowStock_ExcludesInactiveProducts()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        TestSetup.AddProduct(db, sku: "SKU-A001", stock: 2);
        TestSetup.AddProduct(db, sku: "SKU-A002", stock: 1, isActive: false);

        var results = await service.GetLowStockAsync(10);

        Assert.Single(results);
        Assert.Equal("SKU-A001", results.Single().Product.Sku);
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_ExcludesCancelledAndOldOrders()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2);

        db.Orders.AddRange(
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Shipped,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 5, UnitPriceSnapshot = 100m } }
            },
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 100, UnitPriceSnapshot = 100m } }
            },
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Shipped,
                CreatedAt = DateTime.UtcNow.AddDays(-40),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 50, UnitPriceSnapshot = 100m } }
            });
        db.SaveChanges();

        var results = await service.GetLowStockAsync(10);

        Assert.Equal(5, results.Single().SoldLast30Days);
    }

    [Fact]
    public async Task GetLowStock_SoldLast30Days_BoundaryAroundThirtyDays_JustInsideIncludedJustOutsideExcluded()
    {
        using var db = TestSetup.CreateContext();
        var service = TestSetup.CreateProductService(db);
        var customer = TestSetup.AddCustomer(db);
        var product = TestSetup.AddProduct(db, stock: 2);

        db.Orders.AddRange(
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Shipped,
                CreatedAt = DateTime.UtcNow.AddDays(-30).AddHours(1),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 3, UnitPriceSnapshot = 100m } }
            },
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Shipped,
                CreatedAt = DateTime.UtcNow.AddDays(-30).AddHours(-1),
                Items = { new OrderItem { ProductId = product.Id, Quantity = 7, UnitPriceSnapshot = 100m } }
            });
        db.SaveChanges();

        var results = await service.GetLowStockAsync(10);

        Assert.Equal(3, results.Single().SoldLast30Days);
    }
}
