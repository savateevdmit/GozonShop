namespace Orders.Service.Messaging;

/// <summary>
/// Темы сообщений
/// </summary>
public static class Topics
{
    public const string PaymentRequest = "gozon.payments.request.v1";
    public const string PaymentResolved = "gozon.payments.resolved.v1";
}