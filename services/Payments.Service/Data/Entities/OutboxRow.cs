using System.ComponentModel.DataAnnotations;

namespace Payments.Service.Data.Entities;

/// <summary>
/// Запись в таблице исходящих сообщений (outbox)
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