namespace BookSales.Domain.Entities;

public sealed class SalesOrderLine
{
    public long OrderLineId { get; set; }
    public string OrderId { get; set; } = null!;
    public string BookId { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? UnitPrice { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;
    public Book Book { get; set; } = null!;
}
