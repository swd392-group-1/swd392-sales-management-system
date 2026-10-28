namespace BookSalesManagement.Domain.Inventory;

public sealed class BookStock
{
    private BookStock()
    {
        BookId = string.Empty;
    }

    public string BookId { get; private set; }

    public decimal CurrentQuantity { get; private set; }
}
