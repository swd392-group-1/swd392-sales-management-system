namespace BookSales.Domain.Entities;

public sealed class Book
{
    public string BookId { get; set; } = null!;
    public string? Title { get; set; }
    public decimal? CurrentPrice { get; set; }

    public BookStock? Stock { get; set; }
    public ICollection<SalesOrderLine> SalesOrderLines { get; set; } = new List<SalesOrderLine>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
