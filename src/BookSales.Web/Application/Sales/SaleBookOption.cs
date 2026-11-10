namespace BookSales.Application.Sales;

public sealed record SaleBookOption(
    string BookId,
    string? Title,
    decimal? CurrentPrice,
    decimal CurrentQuantity);
