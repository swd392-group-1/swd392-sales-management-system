namespace BookSales.Domain.Entities;

public sealed class InventoryMovement
{
    public long MovementId { get; set; }
    public string PostingKey { get; set; } = null!;
    public string BookId { get; set; } = null!;
    public decimal QuantityDelta { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public Book Book { get; set; } = null!;
}
