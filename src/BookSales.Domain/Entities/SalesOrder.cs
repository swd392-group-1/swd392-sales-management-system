namespace BookSales.Domain.Entities;

public sealed class SalesOrder
{
    public string OrderId { get; set; } = null!;
    public DateOnly? OrderDate { get; set; }
    public decimal? TotalOrderAmount { get; set; }
    public string? CustomerId { get; set; }

    public Customer? Customer { get; set; }
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    public ICollection<OrderPayment> OrderPayments { get; set; } = new List<OrderPayment>();
}
