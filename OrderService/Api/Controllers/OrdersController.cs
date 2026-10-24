using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Orders.Commands.CancelOrder;
using OrderService.Application.Orders.Commands.CreateOrder;
using OrderService.Application.Orders.Common;
using OrderService.Application.Orders.Queries.GetOrderById;
using OrderService.Application.Orders.Queries.GetOrders;

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

    /// <summary>Создать заказ.</summary>
    /// <remarks>
    /// Проверяет наличие товаров в InventoryService, резервирует остатки
    /// и сохраняет заказ со снапшотом цены/имени/SKU на момент оформления.
    /// Если хотя бы одного товара не хватает — возвращает 500 с текстом ошибки.
    /// </remarks>
    /// <response code="201">Заказ создан, в теле — его идентификатор.</response>
    /// <response code="500">Ошибка валидации или резервирования.</response>
    [HttpPost]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
    [FromBody] CreateOrderCommand command, CancellationToken ct)
    {
        try
        {
            var id = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id }, id);
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
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelOrderCommand(id), ct);
        return NoContent();
    }
}