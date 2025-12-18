namespace Orders.Service.Domain;

/// <summary>
/// Статусы заказа
/// </summary>
public static class OrderStatus
{
    public const string New = "NEW";
    public const string Finished = "FINISHED";
    public const string Cancelled = "CANCELLED";
}