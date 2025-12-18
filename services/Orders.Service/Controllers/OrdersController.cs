using Microsoft.AspNetCore.Mvc;
using Orders.Service.UseCases;

namespace Orders.Service.Controllers;

[ApiController]
[Route("orders")]

// Контроллер заказов
public sealed class OrdersController(PlaceOrderFlow placeOrder, ReadOrdersQuery read) : ControllerBase
{
    // создать заказ
    [HttpPost]
    public async Task<IActionResult> Create([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] CreateOrderDto dto, CancellationToken ct)
    {
        if (dto.AmountKopeks <= 0) return BadRequest("amount must be positive");

        var order = await placeOrder.ExecuteAsync(userId, dto.AmountKopeks, dto.Description, ct);
        return Ok(order);
    }

    // список заказов пользователя
    [HttpGet]
    public async Task<IActionResult> List([FromHeader(Name = "X-User-Id")] Guid userId, CancellationToken ct)
        => Ok(await read.ListAsync(userId, ct));

    // получить статус заказа
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> Get([FromHeader(Name = "X-User-Id")] Guid userId, [FromRoute] Guid orderId, CancellationToken ct)
    {
        var order = await read.GetAsync(userId, orderId, ct);
        return order is null ? NotFound() : Ok(order);
    }

    public sealed record CreateOrderDto(long AmountKopeks, string? Description);
}