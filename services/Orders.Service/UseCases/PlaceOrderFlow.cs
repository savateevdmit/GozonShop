using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Orders.Service.Contracts;
using Orders.Service.Data;
using Orders.Service.Data.Entities;
using Orders.Service.Domain;
using Orders.Service.Messaging;

namespace Orders.Service.UseCases;

/// <summary>
/// Процесс размещения заказа
/// </summary>
public sealed class PlaceOrderFlow(OrdersDbContext db)
{
    public async Task<ShopOrder> ExecuteAsync(Guid userId, long amountKopeks, string? description, CancellationToken ct)
    {
        // транзакция на создание заказа и запись события в аутбокс
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var orderId = Guid.NewGuid();

        var row = new OrderRow
        {
            Id = orderId,
            UserId = userId,
            AmountKopeks = amountKopeks,
            Description = description,
            Status = OrderStatus.New,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Orders.Add(row);

        var evt = new PaymentRequestedV1(
            EventId: Guid.NewGuid(),
            OrderId: orderId,
            UserId: userId,
            AmountKopeks: amountKopeks,
            Note: description,
            OccurredAtUtc: DateTimeOffset.UtcNow
        );

        db.Outbox.Add(new OutboxRow
        {
            Id = Guid.NewGuid(),
            Topic = Topics.PaymentRequest,
            MessageKey = orderId.ToString(),
            PayloadJson = JsonSerializer.Serialize(evt),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new ShopOrder(row.Id, row.UserId, row.AmountKopeks, row.Description, row.Status, row.CreatedAtUtc);
    }
}