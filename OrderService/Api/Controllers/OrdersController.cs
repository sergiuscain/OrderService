using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Orders.Commands.CancelOrder;
using OrderService.Application.Orders.Commands.CompleteOrder;
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

    /// <summary>Список заказов текущего пользователя. Админ видит все.</summary>
    /// <response code="200">Список заказов.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken ct)
    {
        if (!TryGetCurrentUser(out var userId, out var isAdmin))
            return Unauthorized(new { error = "Invalid user identifier in token." });

        return Ok(await _mediator.Send(new GetOrdersQuery(userId, isAdmin), ct));
    }

    /// <summary>Заказ по идентификатору. Доступен только владельцу или админу.</summary>
    /// <response code="200">Заказ найден.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Заказ не найден или принадлежит другому пользователю.</response>
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        if (!TryGetCurrentUser(out var userId, out var isAdmin))
            return Unauthorized(new { error = "Invalid user identifier in token." });

        var order = await _mediator.Send(new GetOrderByIdQuery(id, userId, isAdmin), ct);
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
        if (!TryGetCurrentUser(out var userId, out _))
            return Unauthorized(new { error = "Invalid user identifier in token." });

        var command = new CreateOrderCommand(userId, body.Items);
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Завершить заказ (Confirmed → Done). Только владелец или админ.</summary>
    /// <response code="204">Заказ завершён.</response>
    /// <response code="400">Заказ не найден, не принадлежит пользователю или в неверном статусе.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [Authorize]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        if (!TryGetCurrentUser(out var userId, out var isAdmin))
            return Unauthorized(new { error = "Invalid user identifier in token." });

        try
        {
            await _mediator.Send(new CompleteOrderCommand(id, userId, isAdmin), ct);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Отменить заказ и снять резерв в InventoryService.</summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <response code="204">Заказ отменён.</response>
    /// <response code="500">Заказ не найден.</response>
    [Authorize]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelOrderCommand(id), ct);
        return NoContent();
    }
    private bool TryGetCurrentUser(out Guid userId, out bool isAdmin)
    {
        userId = default;
        isAdmin = false;

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(sub, out userId))
            return false;

        isAdmin = User.IsInRole("Admin");
        return true;
    }
}