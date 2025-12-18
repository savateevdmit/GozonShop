using System.ComponentModel.DataAnnotations;

namespace Payments.Service.Data.Entities;

/// <summary>
/// Запись в таблице входящих сообщений (inbox)
/// </summary>
public sealed class InboxRow
{
    [Key]
    public Guid Id { get; set; }

    public string Topic { get; set; } = default!;

    public string MessageKey { get; set; } = default!; // order_id

    public string PayloadJson { get; set; } = default!;

    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}