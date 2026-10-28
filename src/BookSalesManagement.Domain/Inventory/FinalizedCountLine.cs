namespace BookSalesManagement.Domain.Inventory;

public sealed record FinalizedCountLine(
    string BookId,
    long ObservationId,
    decimal ReferenceQuantity,
    decimal CountedQuantity)
{
    public decimal Difference => CountedQuantity - ReferenceQuantity;
}

public sealed class CountFinalization
{
    public CountFinalization(IReadOnlyList<FinalizedCountLine> lines)
    {
        Lines = lines;
    }

    public IReadOnlyList<FinalizedCountLine> Lines { get; }

    public bool HasDifference => Lines.Any(line => line.Difference != 0);
}
