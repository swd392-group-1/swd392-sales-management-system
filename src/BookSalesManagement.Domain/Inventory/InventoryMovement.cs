using BookSalesManagement.Domain.Common;

namespace BookSalesManagement.Domain.Inventory;

public sealed class InventoryMovement
{
    private InventoryMovement()
    {
        PostingKey = string.Empty;
        BookId = string.Empty;
    }

    private InventoryMovement(string postingKey, string bookId, decimal quantityDelta, DateTime occurredAtUtc)
    {
        PostingKey = postingKey;
        BookId = bookId;
        QuantityDelta = quantityDelta;
        OccurredAtUtc = occurredAtUtc;
    }

    public long Id { get; private set; }

    public string PostingKey { get; private set; }

    public string BookId { get; private set; }

    public decimal QuantityDelta { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public static InventoryMovement CreateForAdjustment(
        string adjustmentId,
        AdjustmentBook book,
        DateTime occurredAtUtc)
    {
        if (book.QuantityChange == 0)
        {
            throw new DomainException("Không ghi movement cho chênh lệch bằng 0.");
        }

        var postingKey = $"{adjustmentId}-{book.BookId}";
        if (postingKey.Length > 200)
        {
            throw new DomainException("Posting key vượt quá 200 ký tự.");
        }

        return new InventoryMovement(postingKey, book.BookId, book.QuantityChange, occurredAtUtc);
    }
}
