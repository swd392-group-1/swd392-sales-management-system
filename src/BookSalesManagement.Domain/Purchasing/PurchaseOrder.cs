namespace BookSalesManagement.Domain.Purchasing;

public sealed class PurchaseOrder
{
    private PurchaseOrder()
    {
        Id = string.Empty;
    }

    public string Id { get; private set; }

    public DateOnly? OrderDate { get; private set; }

    public string? Status { get; private set; }

    public List<PurchaseOrderLine> Lines { get; private set; } = new();
}
