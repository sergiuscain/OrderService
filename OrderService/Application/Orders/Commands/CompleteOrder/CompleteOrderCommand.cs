using MediatR;

namespace OrderService.Application.Orders.Commands.CompleteOrder;

public record CompleteOrderCommand(Guid OrderId, Guid UserId, bool IsAdmin) : IRequest;