using MediatR;

namespace OrderService.Application.Orders.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, Guid UserId, bool IsAdmin) : IRequest;