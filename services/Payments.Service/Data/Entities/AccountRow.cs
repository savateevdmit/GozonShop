using System.ComponentModel.DataAnnotations;

namespace Payments.Service.Data.Entities;

/// <summary>
/// Запись о балансе пользователя
/// </summary>
public sealed class AccountRow
{
    [Key]
    public Guid UserId { get; set; }

    public long BalanceKopeks { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}