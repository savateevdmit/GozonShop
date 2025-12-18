using System.ComponentModel.DataAnnotations;

namespace Orders.Service.Data.Entities;

/// <summary>
/// Таблица заказов
/// </summary>
public sealed class OrderRow
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public long AmountKopeks { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = "NEW";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}