namespace BookSales.Application.Returns;

public sealed record BookReturnRequest(
    string ReturnId,
    string OriginalOrderId,
    bool IsCashRefundConfirmed,
    IReadOnlyList<BookReturnLineRequest> Lines);
