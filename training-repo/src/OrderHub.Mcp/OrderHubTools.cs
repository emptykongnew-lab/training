using ModelContextProtocol.Server;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;

[McpServerToolType]
public class OrderHubTools(IOrderService orderService, IProductRepository productRepository)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    [McpServerTool, Description("依訂單編號查詢訂單,含客戶、品項、單價快照、會員折扣與應付總額")]
    public async Task<string> GetOrder([Description("訂單 Id")] int id)
    {
        var order = await orderService.GetOrderAsync(id);
        if (order is null)
            return $"找不到訂單 {id}";

        var tier = order.Customer?.Tier ?? CustomerTier.Standard;
        var subtotal = orderService.CalculateSubtotal(order);
        var total = orderService.CalculateTotal(order);

        var result = new
        {
            order.Id,
            order.CreatedAt,
            Status = order.Status.ToString(),
            Customer = order.Customer is null ? null : new
            {
                order.Customer.Id,
                order.Customer.Name,
                order.Customer.Email,
                Tier = order.Customer.Tier.ToString()
            },
            Items = order.Items.Select(i => new
            {
                ProductSku = i.Product?.Sku,
                ProductName = i.Product?.Name,
                i.Quantity,
                UnitPrice = i.UnitPriceSnapshot,
                LineTotal = i.UnitPriceSnapshot * i.Quantity
            }),
            Subtotal = subtotal,
            DiscountRate = orderService.GetDiscountRate(tier),
            DiscountAmount = subtotal - total,
            Total = total
        };

        return JsonSerializer.Serialize(result, Json);
    }

    [McpServerTool, Description("列出庫存低於門檻且上架中的商品,依庫存數量升冪排序")]
    public async Task<string> LowStock([Description("庫存門檻")] int threshold)
    {
        var products = await productRepository.GetLowStockAsync(threshold);
        var result = products.Select(p => new { p.Sku, p.Name, p.StockQuantity });
        return JsonSerializer.Serialize(result, Json);
    }

    [McpServerTool, Description("查詢某位客戶的全部訂單摘要(編號、日期、狀態、應付總額)")]
    public async Task<string> CustomerOrders([Description("客戶 Id")] int customerId)
    {
        var orders = await orderService.GetCustomerOrdersAsync(customerId);
        var result = orders.Select(o => new
        {
            o.Id,
            o.CreatedAt,
            Status = o.Status.ToString(),
            Total = orderService.CalculateTotal(o)
        });
        return JsonSerializer.Serialize(result, Json);
    }
}
