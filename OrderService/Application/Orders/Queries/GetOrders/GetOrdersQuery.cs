using MediatR;
using OrderService.Application.Orders.Common;

namespace OrderService.Application.Orders.Queries.GetOrders;

public record GetOrdersQuery(Guid UserId, bool IsAdmin) : IRequest<IReadOnlyList<OrderDto>>;