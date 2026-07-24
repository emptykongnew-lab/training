using OrderHub.Core.Domain;

namespace OrderHub.Core.Services;

public record LowStockProductResult(Product Product, int SoldLast30Days);
