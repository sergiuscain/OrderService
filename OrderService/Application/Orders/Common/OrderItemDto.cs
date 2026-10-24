namespace OrderService.Application.Orders.Common;

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);