using Microsoft.EntityFrameworkCore;
using OrderHub.Core.Common;
using OrderHub.Core.Domain;
using OrderHub.Core.Interfaces;
using OrderHub.Infrastructure.Data;

namespace OrderHub.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly OrderHubDbContext _db;

    public OrderRepository(OrderHubDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<Order>> GetPagedAsync(int page, int pageSize, OrderStatus? status)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<Order?> GetWithDetailsAsync(int id) =>
        _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId) =>
        await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<(int ProductId, int Sold)>> GetSoldQuantitiesSinceAsync(DateTime since)
    {
        var rows = await _db.OrderItems
            .Join(_db.Orders,
                item => item.OrderId,
                order => order.Id,
                (item, order) => new { item.ProductId, item.Quantity, order.CreatedAt, order.Status })
            .Where(x => x.CreatedAt >= since && x.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
            .ToListAsync();

        return rows.Select(r => (r.ProductId, r.Sold)).ToList();
    }

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
