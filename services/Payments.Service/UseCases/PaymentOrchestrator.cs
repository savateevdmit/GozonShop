using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Payments.Service.Contracts;
using Payments.Service.Data;
using Payments.Service.Data.Entities;
using Payments.Service.Domain;
using Payments.Service.Messaging;

namespace Payments.Service.UseCases;

/// <summary>
///  Оркестратор обработки платежа с реализацией шаблонов inbox/outbox и бухгалтерской книги
/// </summary>
/// <param name="db"></param>
public sealed class PaymentOrchestrator(PaymentsDbContext db)
{
  public async Task<bool> HandleAsync(string topic, string messageKey, string payloadJson, CancellationToken ct)
  {
    // inbox pattern: проверить, не обработано ли сообщение уже
    await using var tx = await db.Database.BeginTransactionAsync(ct);

    var already = await db.Inbox.AnyAsync(x => x.Topic == topic && x.MessageKey == messageKey, ct);
    if (already)
    {
      await tx.CommitAsync(ct);
      return true;
    }

    db.Inbox.Add(new InboxRow
    {
      Id = Guid.NewGuid(),
      Topic = topic,
      MessageKey = messageKey,
      PayloadJson = payloadJson,
      ReceivedAtUtc = DateTimeOffset.UtcNow
    });

    var evt = JsonSerializer.Deserialize<PaymentRequestedV1>(payloadJson);
    if (evt is null)
    {
      await tx.CommitAsync(ct);
      return true;
    }

    var result = await TryDebitOrderAsync(evt, ct);

    db.Outbox.Add(new OutboxRow
    {
      Id = Guid.NewGuid(),
      Topic = Topics.PaymentResolved,
      MessageKey = evt.OrderId.ToString(),
      PayloadJson = JsonSerializer.Serialize(result),
      CreatedAtUtc = DateTimeOffset.UtcNow
    });

    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return true;
  }

  private async Task<PaymentResolvedV1> TryDebitOrderAsync(PaymentRequestedV1 evt, CancellationToken ct)
  {
    var account = await db.Accounts.FirstOrDefaultAsync(x => x.UserId == evt.UserId, ct);
    if (account is null)
      return new PaymentResolvedV1(Guid.NewGuid(), evt.OrderId, evt.UserId, "FAIL", "account_missing", DateTimeOffset.UtcNow);

    var orderRef = evt.OrderId.ToString();

    // проверка idемпотентности по внешнему ключу (order id)
    var alreadyDebited = await db.Ledger.AnyAsync(x =>
      x.Kind == LedgerOperationKind.DebitOrder && x.ExternalRef == orderRef, ct);

    if (alreadyDebited)
      return new PaymentResolvedV1(Guid.NewGuid(), evt.OrderId, evt.UserId, "SUCCESS", "already_processed", DateTimeOffset.UtcNow);

    // списание средств с баланса
    var updated = await db.Database.ExecuteSqlInterpolatedAsync($@"
      update ""Accounts""
      set ""BalanceKopeks"" = ""BalanceKopeks"" - {evt.AmountKopeks},
          ""UpdatedAtUtc"" = {DateTimeOffset.UtcNow}
      where ""UserId"" = {evt.UserId}
        and ""BalanceKopeks"" >= {evt.AmountKopeks}
    ", ct);

    if (updated == 0)
      return new PaymentResolvedV1(Guid.NewGuid(), evt.OrderId, evt.UserId, "FAIL", "insufficient_funds", DateTimeOffset.UtcNow);

    db.Ledger.Add(new LedgerRow
    {
      Id = Guid.NewGuid(),
      UserId = evt.UserId,
      OrderId = evt.OrderId,
      Kind = LedgerOperationKind.DebitOrder,
      ExternalRef = orderRef,
      AmountKopeks = evt.AmountKopeks,
      CreatedAtUtc = DateTimeOffset.UtcNow
    });

    return new PaymentResolvedV1(Guid.NewGuid(), evt.OrderId, evt.UserId, "SUCCESS", null, DateTimeOffset.UtcNow);
  }
}