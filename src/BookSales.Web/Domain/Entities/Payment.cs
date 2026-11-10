namespace BookSales.Domain.Entities;

public sealed class Payment
{
    public string PaymentId { get; set; } = null!;
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }

    public ICollection<OrderPayment> OrderPayments { get; set; } = new List<OrderPayment>();
}
