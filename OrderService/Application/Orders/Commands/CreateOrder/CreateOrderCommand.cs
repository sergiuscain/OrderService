using MediatR;

namespace OrderService.Application.Orders.Commands.CreateOrder;

public record CreateOrderItemRequest(Guid ProductId, int Quantity);

public record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyList<CreateOrderItemRequest> Items) : IRequest<Guid>;