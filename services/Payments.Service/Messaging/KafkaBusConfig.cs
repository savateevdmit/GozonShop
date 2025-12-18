using Confluent.Kafka;

namespace Payments.Service.Messaging;

/// <summary>
/// Конфигурация Kafka для продюсеров и консумеров
/// </summary>
public static class KafkaBusConfig
{
    public static ProducerConfig BuildProducer(IConfiguration cfg) =>
        new()
        {
            BootstrapServers = cfg["Kafka:BootstrapServers"]
        };

    public static ConsumerConfig BuildConsumer(IConfiguration cfg, string groupId) =>
        new()
        {
            BootstrapServers = cfg["Kafka:BootstrapServers"],
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
}