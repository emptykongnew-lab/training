using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;

namespace OrderHub.Core.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;

    public ProductService(IProductRepository productRepository, IOrderRepository orderRepository)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public Task<IReadOnlyList<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<IReadOnlyList<Product>> GetActiveAsync() => _productRepository.GetActiveAsync();

    public async Task<IReadOnlyList<LowStockProductResult>> GetLowStockAsync(int threshold)
    {
        var products = await _productRepository.GetLowStockAsync(threshold);
        var soldQuantities = (await _orderRepository.GetSoldQuantitiesSinceAsync(DateTime.UtcNow.AddDays(-30)))
            .ToDictionary(x => x.ProductId, x => x.Sold);

        return products
            .Select(p => new LowStockProductResult(p, soldQuantities.GetValueOrDefault(p.Id, 0)))
            .ToList();
    }
}
