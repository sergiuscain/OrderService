using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Orders.Common;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderDbContext _db;

    public GetOrderByIdQueryHandler(OrderDbContext db) => _db = db;

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.Id == request.Id);

        if (!request.IsAdmin)
            query = query.Where(o => o.CustomerId == request.UserId);

        return await query
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
            .FirstOrDefaultAsync(ct);
    }
}