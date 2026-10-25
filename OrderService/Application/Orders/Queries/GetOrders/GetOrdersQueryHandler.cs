using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Orders.Common;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly OrderDbContext _db;

    public GetOrdersQueryHandler(OrderDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking();

        if (!request.IsAdmin)
            query = query.Where(o => o.CustomerId == request.UserId);

        return await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderDto(
                o.Id,
                o.CustomerId,
                o.Status,
                o.TotalAmount,
                o.CreatedAt,
                o.UpdatedAt,
                o.Items.Select(i => new OrderItemDto(
                    i.Id, i.ProductId, i.ProductName, i.Sku,
                    i.Quantity, i.UnitPrice, i.LineTotal)).ToList()))
            .ToListAsync(ct);
    }
}