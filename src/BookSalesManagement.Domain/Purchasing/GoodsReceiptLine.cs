namespace BookSalesManagement.Domain.Purchasing;

public sealed class GoodsReceiptLine
{
    private GoodsReceiptLine()
    {
        ReceiptId = string.Empty;
    }

    public long Id { get; private set; }

    public string ReceiptId { get; private set; }

    public long PurchaseOrderLineId { get; private set; }

    public decimal ReceivedQuantity { get; private set; }
}
