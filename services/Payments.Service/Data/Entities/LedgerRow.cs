using System.ComponentModel.DataAnnotations;

namespace Payments.Service.Data.Entities;

/// <summary>
/// Запись в бухгалтерской книге
/// </summary>
public sealed class LedgerRow
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? OrderId { get; set; } // nullable for non-order operations

    public string Kind { get; set; } = default!; // DEBIT_ORDER or TOP_UP

    public string ExternalRef { get; set; } = default!; // idempotency key

    public long AmountKopeks { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}