namespace BookSales.Application.Sales;

public enum ProcessSaleStatus
{
    Completed,
    AlreadyProcessed,
    InvalidRequest,
    CustomerNotFound,
    InsufficientStock,
    OrderConflict,
    PaymentPending
}

public sealed record ProcessSaleLineResult(
    string BookId,
    string? Title,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record ProcessSaleResult(
    ProcessSaleStatus Status,
    string OrderId,
    decimal? TotalAmount,
    IReadOnlyList<ProcessSaleLineResult> Lines,
    string? Error,
    string? PaymentMethod,
    string? PaymentStatus)
{
    public bool IsSuccessful => Status is ProcessSaleStatus.Completed or ProcessSaleStatus.AlreadyProcessed;
}
