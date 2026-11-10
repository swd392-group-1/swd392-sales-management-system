namespace BookSales.Domain.Entities;

public sealed class Customer
{
    public string CustomerId { get; set; } = null!;

    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
