namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class StockReconciliationView
{
    private StockReconciliationView()
    {
        BookId = string.Empty;
    }

    public string BookId { get; private set; }

    public decimal CurrentQuantity { get; private set; }

    public decimal LedgerQuantity { get; private set; }

    public decimal Difference { get; private set; }
}
