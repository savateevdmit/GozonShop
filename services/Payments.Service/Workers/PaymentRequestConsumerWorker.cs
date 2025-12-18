using Confluent.Kafka;
using Payments.Service.Messaging;
using Payments.Service.UseCases;

namespace Payments.Service.Workers;

/// <summary>
/// Фоновый сервис для обработки запросов на оплату из Kafka
/// </summary>
public sealed class PaymentRequestConsumerWorker(IServiceScopeFactory scopeFactory, IConsumer<string, string> consumer)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(Topics.PaymentRequest);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                if (cr?.Message?.Value is null) continue;

                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<PaymentOrchestrator>();

                await orchestrator.HandleAsync(
                    topic: Topics.PaymentRequest,
                    messageKey: cr.Message.Key ?? "",
                    payloadJson: cr.Message.Value,
                    ct: stoppingToken
                );

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

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        consumer.Close();
        consumer.Dispose();
        return base.StopAsync(cancellationToken);
    }
}