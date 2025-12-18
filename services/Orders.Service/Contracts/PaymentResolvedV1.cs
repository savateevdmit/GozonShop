namespace Orders.Service.Contracts;

/// <summary>
/// Событие о результате оплаты заказа
/// </summary>
public sealed record PaymentResolvedV1(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    string Outcome,
    string? Reason,
    DateTimeOffset OccurredAtUtc
);