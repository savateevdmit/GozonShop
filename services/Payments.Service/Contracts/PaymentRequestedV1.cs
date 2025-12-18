namespace Payments.Service.Contracts;

/// <summary>
/// Событие о запросе платежа
/// </summary>
/// <param name="EventId"></param>
/// <param name="OrderId"></param>
/// <param name="UserId"></param>
/// <param name="AmountKopeks"></param>
/// <param name="Note"></param>
/// <param name="OccurredAtUtc"></param>
public sealed record PaymentRequestedV1(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    long AmountKopeks,
    string? Note,
    DateTimeOffset OccurredAtUtc
);