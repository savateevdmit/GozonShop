using Microsoft.EntityFrameworkCore;
using Orders.Service.Data;
using Orders.Service.Domain;

namespace Orders.Service.UseCases;

/// <summary>
/// Запросы на чтение заказов
/// </summary>
public sealed class ReadOrdersQuery(OrdersDbContext db)
{
    public async Task<IReadOnlyList<ShopOrder>> ListAsync(Guid userId, CancellationToken ct)
    {
        var rows = await db.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        return rows.Select(x => new ShopOrder(x.Id, x.UserId, x.AmountKopeks, x.Description, x.Status, x.CreatedAtUtc)).ToList();
    }

    public async Task<ShopOrder?> GetAsync(Guid userId, Guid orderId, CancellationToken ct)
    {
        var row = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Id == orderId, ct);
        return row is null ? null : new ShopOrder(row.Id, row.UserId, row.AmountKopeks, row.Description, row.Status, row.CreatedAtUtc);
    }
}