namespace BookSalesManagement.Domain.Purchasing;

public sealed class PurchaseOrderProvider
{
    private PurchaseOrderProvider()
    {
        PurchaseOrderId = string.Empty;
        ProviderId = string.Empty;
    }

    public string PurchaseOrderId { get; private set; }

    public string ProviderId { get; private set; }
}
