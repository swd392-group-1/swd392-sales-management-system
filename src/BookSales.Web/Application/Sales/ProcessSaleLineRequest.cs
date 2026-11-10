namespace BookSales.Application.Sales;

public sealed record ProcessSaleLineRequest(string BookId, decimal Quantity);
