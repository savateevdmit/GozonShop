namespace Payments.Service.Domain;

/// <summary>
/// Виды операций в бухгалтерской книге
/// </summary>
public static class LedgerOperationKind
{
    public const string DebitOrder = "DEBIT_ORDER";
    public const string TopUp = "TOP_UP";
}