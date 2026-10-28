namespace BookSalesManagement.Domain.Purchasing;

public sealed class GoodsReceipt
{
    private GoodsReceipt()
    {
        Id = string.Empty;
    }

    public string Id { get; private set; }

    public DateOnly? ReceiptDate { get; private set; }

    public List<GoodsReceiptLine> Lines { get; private set; } = new();
}
