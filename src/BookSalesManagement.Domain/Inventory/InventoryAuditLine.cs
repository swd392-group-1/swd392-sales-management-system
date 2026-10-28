namespace BookSalesManagement.Domain.Inventory;

public sealed class InventoryAuditLine
{
    private InventoryAuditLine()
    {
        AuditId = string.Empty;
        BookId = string.Empty;
    }

    internal InventoryAuditLine(string auditId, string bookId, decimal countedQuantity)
    {
        AuditId = auditId;
        BookId = bookId;
        CountedQuantity = countedQuantity;
    }

    public long Id { get; private set; }

    public string AuditId { get; private set; }

    public string BookId { get; private set; }

    public decimal CountedQuantity { get; private set; }
}
