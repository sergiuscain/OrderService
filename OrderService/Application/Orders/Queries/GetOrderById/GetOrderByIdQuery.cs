using MediatR;
using OrderService.Application.Orders.Common;

namespace OrderService.Application.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(Guid Id, Guid UserId, bool IsAdmin) : IRequest<OrderDto?>;