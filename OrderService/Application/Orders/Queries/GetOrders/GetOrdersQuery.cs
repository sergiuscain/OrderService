using MediatR;
using OrderService.Application.Orders.Common;

namespace OrderService.Application.Orders.Queries.GetOrders;

public record GetOrdersQuery : IRequest<IReadOnlyList<OrderDto>>;