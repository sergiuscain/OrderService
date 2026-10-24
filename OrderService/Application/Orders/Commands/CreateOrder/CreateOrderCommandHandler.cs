using MediatR;
using OrderService.Application.Orders.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly OrderDbContext _db;
    private readonly IInventoryService _inventory;

    public CreateOrderCommandHandler(OrderDbContext db, IInventoryService inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count == 0)
            throw new InvalidOperationException("Order must contain at least one item.");

        foreach (var item in request.Items)
            if (item.Quantity <= 0)
                throw new InvalidOperationException("Quantity must be positive.");

        // Схлопываем дубли по ProductId.
        var quantities = request.Items
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var productIds = quantities.Keys.ToList();
        var products = await _inventory.GetProductsAsync(productIds, ct);
        var byId = products.ToDictionary(p => p.Id);

        foreach (var id in productIds)
            if (!byId.ContainsKey(id))
                throw new InvalidOperationException($"Product {id} not found in inventory.");

        // Проверяем остатки до резерва.
        foreach (var (productId, qty) in quantities)
        {
            var p = byId[productId];
            if (p.QuantityAvailable < qty)
                throw new InvalidOperationException(
                    $"Not enough stock for '{p.Name}': requested {qty}, available {p.QuantityAvailable}.");
        }

        var reserved = await _inventory.TryReserveAsync(quantities, ct);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Status = reserved ? OrderStatus.Confirmed : OrderStatus.Failed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var p = byId[item.ProductId];
            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                Quantity = item.Quantity,
                UnitPrice = p.Price
            });
        }

        order.RecalculateTotal();

        _db.Orders.Add(order);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Компенсация: сохранение упало — снимаем резерв, чтобы товар не «завис».
            if (reserved)
                await _inventory.ReleaseAsync(quantities, ct);
            throw;
        }

        return order.Id;
    }
}