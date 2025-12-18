using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Orders.Service.Data;

namespace Orders.Service.Workers;

/// <summary>
/// Фоновый сервис для публикации исходящих сообщений из таблицы outbox
/// </summary>
/// <param name="scopeFactory"></param>
/// <param name="producer"></param>
public sealed class OutboxPublisherWorker(IServiceScopeFactory scopeFactory, IProducer<string, string> producer)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch
            {
                // minimal noise, retry loop
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var batch = await db.Outbox
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(ct);

        if (batch.Count == 0) return;

        foreach (var msg in batch)
        {
            try
            {
                await producer.ProduceAsync(msg.Topic, new Message<string, string>
                {
                    Key = msg.MessageKey,
                    Value = msg.PayloadJson
                }, ct);

                msg.PublishedAtUtc = DateTimeOffset.UtcNow;
            }
            catch
            {
                msg.Attempts += 1;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}