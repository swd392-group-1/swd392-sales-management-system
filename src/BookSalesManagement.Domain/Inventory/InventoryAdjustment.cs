using BookSalesManagement.Domain.Common;

namespace BookSalesManagement.Domain.Inventory;

public sealed class InventoryAdjustment
{
    private InventoryAdjustment()
    {
        Id = string.Empty;
    }

    private InventoryAdjustment(string id, DateOnly adjustmentDate, string? reason)
    {
        Id = id;
        AdjustmentDate = adjustmentDate;
        Reason = reason;
    }

    public string Id { get; private set; }

    public DateOnly? AdjustmentDate { get; private set; }

    public string? Reason { get; private set; }

    public List<AdjustmentBook> Books { get; private set; } = new();

    public static InventoryAdjustment? CreateForCount(
        string countId,
        string adjustmentId,
        DateTime occurredAtUtc,
        IReadOnlyList<FinalizedCountLine> lines)
    {
        var differences = lines.Where(line => line.Difference != 0).ToList();
        if (differences.Count == 0)
        {
            return null;
        }

        var adjustment = new InventoryAdjustment(
            adjustmentId,
            DateOnly.FromDateTime(occurredAtUtc),
            $"Chênh lệch kiểm kê {countId}");

        foreach (var difference in differences)
        {
            adjustment.Books.Add(new AdjustmentBook(adjustmentId, difference.BookId, difference.Difference));
        }

        return adjustment;
    }
}
