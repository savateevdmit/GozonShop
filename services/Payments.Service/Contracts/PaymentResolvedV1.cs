namespace Payments.Service.Contracts;

/// <summary>
/// Событие о завершении обработки платежа
/// </summary>
/// <param name="EventId"></param>
/// <param name="OrderId"></param>
/// <param name="UserId"></param>
/// <param name="Outcome"></param>
/// <param name="Reason"></param>
/// <param name="OccurredAtUtc"></param>
public sealed record PaymentResolvedV1(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    string Outcome,
    string? Reason,
    DateTimeOffset OccurredAtUtc
);