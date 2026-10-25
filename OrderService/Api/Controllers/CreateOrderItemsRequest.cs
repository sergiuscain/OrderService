using OrderService.Application.Orders.Commands.CreateOrder;

namespace OrderService.Api.Controllers;

public record CreateOrderItemsRequest(IReadOnlyList<CreateOrderItemRequest> Items);