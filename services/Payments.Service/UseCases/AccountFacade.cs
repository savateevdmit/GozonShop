using Microsoft.EntityFrameworkCore;
using Payments.Service.Data;
using Payments.Service.Data.Entities;
using Payments.Service.Domain;

namespace Payments.Service.UseCases;

/// <summary>
/// Фасад для работы со счетами пользователей
/// </summary>
/// <param name="db"></param>
public sealed class AccountFacade(PaymentsDbContext db)
{
    public async Task<bool> EnsureAccountAsync(Guid userId, CancellationToken ct)
    {
        // если счет уже есть, ничего не делать
        var exists = await db.Accounts.AnyAsync(x => x.UserId == userId, ct);
        if (exists) return false;

        db.Accounts.Add(new AccountRow
        {
            UserId = userId,
            BalanceKopeks = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<long?> GetBalanceAsync(Guid userId, CancellationToken ct)
    {
        var row = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return row?.BalanceKopeks;
    }

    public async Task<bool> TopUpAsync(Guid userId, long amountKopeks, CancellationToken ct)
    {
        if (amountKopeks <= 0) return false;

        var row = await db.Accounts.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (row is null) return false;

        row.BalanceKopeks += amountKopeks;
        row.UpdatedAtUtc = DateTimeOffset.UtcNow;

        db.Ledger.Add(new LedgerRow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderId = null,
            Kind = LedgerOperationKind.TopUp,
            ExternalRef = Guid.NewGuid().ToString(),
            AmountKopeks = amountKopeks,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return true;
    }
}