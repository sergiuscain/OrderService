using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Orders.Events;
using OrderService.Domain.Enums;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Application.Orders.Commands.CompleteOrder;

public sealed class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand>
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public CompleteOrderCommandHandler(OrderDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(CompleteOrderCommand request, CancellationToken ct)
    {
        var query = _db.Orders
            .Include(o => o.Items)
            .Where(o => o.Id == request.OrderId);

        if (!request.IsAdmin)
            query = query.Where(o => o.CustomerId == request.UserId);

        var order = await query.FirstOrDefaultAsync(ct)
            ?? throw new ArgumentException($"Order {request.OrderId} not found.");

        if (order.Status == OrderStatus.Done)
            return;

        if (order.Status != OrderStatus.Confirmed)
            throw new ArgumentException(
                $"Cannot complete order in status '{order.Status}'.");

        order.Status = OrderStatus.Done;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var items = order.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new OrderCompletedItem(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        await _publishEndpoint.Publish(new OrderCompletedEvent(order.Id, items), ct);
    }
}