namespace BookSalesManagement.Domain.Purchasing;

public sealed class PurchaseOrderLine
{
    private PurchaseOrderLine()
    {
        PurchaseOrderId = string.Empty;
        BookId = string.Empty;
    }

    public long Id { get; private set; }

    public string PurchaseOrderId { get; private set; }

    public string BookId { get; private set; }

    public decimal OrderedQuantity { get; private set; }
}
