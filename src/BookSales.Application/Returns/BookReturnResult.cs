namespace BookSales.Application.Returns;

public enum BookReturnStatus
{
    Completed,
    AlreadyProcessed,
    InvalidRequest,
    OrderNotFound,
    PaymentNotCompleted,
    ReturnWindowExpired,
    OrderLineNotFound,
    QuantityExceedsRemaining,
    OriginalPriceUnavailable,
    BookNotNew,
    StockUnavailable,
    Conflict
}

public sealed record ReturnableOrderLine(
    long OriginalOrderLineId,
    string BookId,
    string? Title,
    decimal PurchasedQuantity,
    decimal PreviouslyReturnedQuantity,
    decimal RemainingQuantity,
    decimal? OriginalUnitPrice);

public sealed record ReturnableOrderDetails(
    string OrderId,
    DateOnly? OrderDate,
    decimal? TotalOrderAmount,
    string? PaymentMethod,
    string? PaymentStatus,
    bool IsPaid,
    bool IsWithinReturnWindow,
    string? IneligibilityMessage,
    IReadOnlyList<ReturnableOrderLine> Lines)
{
    public bool CanReturn =>
        IsPaid &&
        IsWithinReturnWindow &&
        Lines.Any(line =>
            line.RemainingQuantity > 0m &&
            line.OriginalUnitPrice is > 0m);
}

public sealed record BookReturnLineResult(
    long OriginalOrderLineId,
    string BookId,
    string? Title,
    decimal ReturnedQuantity,
    decimal UnitPrice,
    decimal RefundAmount);

public sealed record BookReturnResult(
    BookReturnStatus Status,
    string ReturnId,
    string OriginalOrderId,
    DateTime? ReturnedAtUtc,
    string? OriginalPaymentMethod,
    IReadOnlyList<BookReturnLineResult> Lines,
    decimal RefundAmount,
    string? Error)
{
    public bool IsSuccessful =>
        Status is BookReturnStatus.Completed or BookReturnStatus.AlreadyProcessed;

    public string RefundMethod => "Cash";
}

public sealed record BookReturnSummary(
    string ReturnId,
    string OriginalOrderId,
    DateTime ReturnedAtUtc,
    decimal RefundAmount);
