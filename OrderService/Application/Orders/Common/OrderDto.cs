using OrderService.Domain.Enums;

namespace OrderService.Application.Orders.Common;

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemDto> Items);