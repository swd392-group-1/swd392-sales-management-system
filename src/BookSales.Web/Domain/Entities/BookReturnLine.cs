namespace BookSales.Domain.Entities;

public sealed class BookReturnLine
{
    public long ReturnLineId { get; set; }
    public string ReturnId { get; set; } = null!;
    public long OriginalOrderLineId { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal RefundAmount { get; set; }

    public BookReturn BookReturn { get; set; } = null!;
    public SalesOrderLine OriginalOrderLine { get; set; } = null!;
}
