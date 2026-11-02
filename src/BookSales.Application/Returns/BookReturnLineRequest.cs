namespace BookSales.Application.Returns;

public sealed record BookReturnLineRequest(
    long OriginalOrderLineId,
    decimal ReturnQuantity,
    bool IsBookNewConfirmed);
