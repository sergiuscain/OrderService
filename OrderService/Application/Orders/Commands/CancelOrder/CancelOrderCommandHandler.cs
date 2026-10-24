using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Orders.Services;
using OrderService.Domain.Enums;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly OrderDbContext _db;
    private readonly IInventoryService _inventory;

    public CancelOrderCommandHandler(OrderDbContext db, IInventoryService inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new InvalidOperationException($"Order {request.OrderId} not found.");

        if (order.Status == OrderStatus.Cancelled)
            return;

        // Снимаем резерв, только если он был выставлен.
        if (order.Status is OrderStatus.Confirmed or OrderStatus.Pending)
        {
            var quantities = order.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
            await _inventory.ReleaseAsync(quantities, ct);
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}