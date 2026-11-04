namespace BookSales.Domain.Entities;

public sealed class OrderPayment
{
    public string OrderId { get; set; } = null!;
    public string PaymentId { get; set; } = null!;

    public SalesOrder SalesOrder { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
}
