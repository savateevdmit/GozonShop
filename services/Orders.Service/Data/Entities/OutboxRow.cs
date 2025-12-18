using System.ComponentModel.DataAnnotations;

namespace Orders.Service.Data.Entities;

/// <summary>
/// Таблица исходящих сообщений (Outbox Pattern)
/// </summary>
public sealed class OutboxRow
{
    [Key]
    public Guid Id { get; set; }

    public string Topic { get; set; } = default!;

    public string MessageKey { get; set; } = default!;

    public string PayloadJson { get; set; } = default!;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public int Attempts { get; set; }
}