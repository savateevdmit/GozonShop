using Microsoft.AspNetCore.Mvc;
using Payments.Service.UseCases;

namespace Payments.Service.Controllers;

[ApiController]
[Route("payments/accounts")]

// Контроллер для управления счетами пользователей
public sealed class AccountsController(AccountFacade accounts) : ControllerBase
{
    // создать счет, если не существует
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromHeader(Name = "X-User-Id")] Guid userId, CancellationToken ct)
    {
        var created = await accounts.EnsureAccountAsync(userId, ct);
        return Ok(new { created });
    }

    // пополнить счет
    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] TopUpDto dto, CancellationToken ct)
    {
        var ok = await accounts.TopUpAsync(userId, dto.AmountKopeks, ct);
        return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false });
    }

    // получить баланс счета
    [HttpGet("balance")]
    public async Task<IActionResult> Balance([FromHeader(Name = "X-User-Id")] Guid userId, CancellationToken ct)
    {
        // если счета нет, вернуть null
        var balance = await accounts.GetBalanceAsync(userId, ct);
        return Ok(new
        {
            exists = balance is not null,
            balanceKopeks = balance ?? 0
        });
    }

    public sealed record TopUpDto(long AmountKopeks);
}