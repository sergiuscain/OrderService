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
        var query = _db.Orders
            .Include(o => o.Items)
            .Where(o => o.Id == request.OrderId);

        if (!request.IsAdmin)
            query = query.Where(o => o.CustomerId == request.UserId);

        var order = await query.FirstOrDefaultAsync(ct)
            ?? throw new ArgumentException($"Order {request.OrderId} not found.");

        if (order.Status == OrderStatus.Cancelled)
            return; // идемпотентно

        // Снимаем резерв, только если он был выставлен
        // (Pending — резерв не прошёл, снимать нечего; Done — товар уже списан, отменять нельзя).
        if (order.Status == OrderStatus.Confirmed)
        {
            var quantities = order.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            await _inventory.ReleaseAsync(quantities, ct);
        }
        else if (order.Status == OrderStatus.Done)
        {
            throw new ArgumentException("Cannot cancel a completed order.");
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}