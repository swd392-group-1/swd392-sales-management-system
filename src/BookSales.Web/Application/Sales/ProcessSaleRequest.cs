namespace BookSales.Application.Sales;

public sealed record ProcessSaleRequest(
    string OrderId,
    string? CustomerId,
    IReadOnlyList<ProcessSaleLineRequest> Lines);
