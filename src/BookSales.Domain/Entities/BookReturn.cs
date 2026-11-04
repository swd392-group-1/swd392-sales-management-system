namespace BookSales.Domain.Entities;

public sealed class BookReturn
{
    public string ReturnId { get; set; } = null!;
    public string OriginalOrderId { get; set; } = null!;
    public DateTime ReturnedAtUtc { get; set; }

    public SalesOrder OriginalOrder { get; set; } = null!;
    public ICollection<BookReturnLine> Lines { get; set; } = new List<BookReturnLine>();
}
