using Confluent.Kafka;

namespace Orders.Service.Messaging;

/// <summary>
/// Конфигурация Kafka
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