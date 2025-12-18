using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Orders.Service.Contracts;
using Orders.Service.Data;
using Orders.Service.Domain;
using Orders.Service.Messaging;

namespace Orders.Service.Workers;

/// <summary>
/// Фоновый сервис для обработки результатов платежей
/// </summary>
/// <param name="scopeFactory"></param>
/// <param name="consumer"></param>
public sealed class PaymentResultConsumerWorker(IServiceScopeFactory scopeFactory, IConsumer<string, string> consumer)
  : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    consumer.Subscribe(Topics.PaymentResolved);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var cr = consumer.Consume(stoppingToken);
        if (cr?.Message?.Value is null) continue;

        var evt = JsonSerializer.Deserialize<PaymentResolvedV1>(cr.Message.Value);
        if (evt is null)
        {
          consumer.Commit(cr);
          continue;
        }

        await ApplyAsync(evt, stoppingToken);

        consumer.Commit(cr);
      }
      catch (OperationCanceledException)
      {
        break;
      }
      catch
      {
        await Task.Delay(300, stoppingToken);
      }
    }
  }

  private async Task ApplyAsync(PaymentResolvedV1 evt, CancellationToken ct)
  {
    using var scope = scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

    var row = await db.Orders.FirstOrDefaultAsync(x => x.Id == evt.OrderId && x.UserId == evt.UserId, ct);
    if (row is null) return;

    // обновляем статус заказа только если он в состоянии New
    if (row.Status != OrderStatus.New) return;

    row.Status = evt.Outcome == "SUCCESS" ? OrderStatus.Finished : OrderStatus.Cancelled;
    row.UpdatedAtUtc = DateTimeOffset.UtcNow;

    await db.SaveChangesAsync(ct);
  }

  public override Task StopAsync(CancellationToken cancellationToken)
  {
    consumer.Close();
    consumer.Dispose();
    return base.StopAsync(cancellationToken);
  }
}