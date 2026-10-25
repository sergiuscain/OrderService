using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Orders.Commands.CancelOrder;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Application.Orders.Common;
using OrderService.Application.Orders.Queries.GetOrderById;
using OrderService.Application.Orders.Queries.GetOrders;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OrderService.Api.Controllers;

/// <summary>Управление заказами.</summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Получить список всех заказов (новые сверху).</summary>
    /// <response code="200">Список заказов.</response>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetOrdersQuery(), ct));

    /// <summary>Получить заказ по идентификатору.</summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <response code="200">Заказ найден.</response>
    /// <response code="404">Заказ не найден.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Создать заказ. Требует аутентификации; customerId берётся из токена.</summary>
    /// <response code="201">Заказ создан.</response>
    /// <response code="400">Ошибка валидации или недостаточно остатков.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateOrderItemsRequest body, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(sub, out var customerId))
            return Unauthorized(new { error = "Invalid user identifier in token." });

        var command = new CreateOrderCommand(customerId, body.Items);
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Отменить заказ и снять резерв в InventoryService.</summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <response code="204">Заказ отменён.</response>
    /// <response code="500">Заказ не найден.</response>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelOrderCommand(id), ct);
        return NoContent();
    }
}