namespace Orders.Service.Domain;

/// <summary>
/// Заказ в магазине
/// </summary>
/// <param name="Id"></param>
/// <param name="UserId"></param>
/// <param name="AmountKopeks"></param>
/// <param name="Description"></param>
/// <param name="Status"></param>
/// <param name="CreatedAtUtc"></param>
public sealed record ShopOrder(
    Guid Id,
    Guid UserId,
    long AmountKopeks,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc
);