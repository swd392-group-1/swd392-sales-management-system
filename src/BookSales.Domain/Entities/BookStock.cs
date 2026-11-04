namespace BookSales.Domain.Entities;

public sealed class BookStock
{
    public string BookId { get; set; } = null!;
    public decimal CurrentQuantity { get; set; }

    public Book Book { get; set; } = null!;
}
