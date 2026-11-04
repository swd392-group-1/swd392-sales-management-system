namespace BookSales.Domain.Entities;

public sealed class Book
{
    public string BookId { get; set; } = null!;
    public string? Title { get; set; }
    public decimal? CurrentPrice { get; set; }

    public void ChangeCurrentPrice(decimal newPrice)
    {
        if (newPrice < 0 || newPrice >= 1000000000000000m || decimal.Round(newPrice, 4) != newPrice)
            throw new ArgumentException("Price must be nonnegative with at most four decimal places.");
        CurrentPrice = newPrice;
    }

    public BookStock? Stock { get; set; }
    public ICollection<SalesOrderLine> SalesOrderLines { get; set; } = new List<SalesOrderLine>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
