namespace BookSalesManagement.Domain.Inventory;

public sealed class InventoryCountSelection
{
    private InventoryCountSelection()
    {
        CountId = string.Empty;
        BookId = string.Empty;
    }

    internal InventoryCountSelection(string countId, string bookId, long selectedObservationId, decimal recordedQuantity)
    {
        CountId = countId;
        BookId = bookId;
        SelectedObservationId = selectedObservationId;
        RecordedQuantity = recordedQuantity;
    }

    public string CountId { get; private set; }

    public string BookId { get; private set; }

    public long SelectedObservationId { get; private set; }

    public decimal RecordedQuantity { get; private set; }
}
