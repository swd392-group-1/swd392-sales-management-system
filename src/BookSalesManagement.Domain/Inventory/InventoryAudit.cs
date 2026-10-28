using BookSalesManagement.Domain.Common;

namespace BookSalesManagement.Domain.Inventory;

public sealed class InventoryAudit
{
    public const string DraftStatus = "DRAFT";
    public const string ApprovedStatus = "APPROVED";

    private InventoryAudit()
    {
        Id = string.Empty;
    }

    public InventoryAudit(string id, DateOnly auditDate)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new DomainException("Mã đợt audit không hợp lệ.");
        }

        Id = id;
        AuditDate = auditDate;
        Status = DraftStatus;
    }

    public string Id { get; private set; }

    public DateOnly? AuditDate { get; private set; }

    public string? Status { get; private set; }

    public List<InventoryAuditLine> Lines { get; private set; } = new();

    public bool IsApproved => string.Equals(Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase);

    public void AddLine(string bookId, decimal countedQuantity)
    {
        if (IsApproved)
        {
            throw new DomainException("Đợt audit đã được duyệt, không thể thêm dòng.");
        }

        if (string.IsNullOrWhiteSpace(bookId))
        {
            throw new DomainException("Book của dòng audit không hợp lệ.");
        }

        if (countedQuantity < 0)
        {
            throw new DomainException("Số lượng đếm không được âm.");
        }

        Lines.Add(new InventoryAuditLine(Id, bookId, countedQuantity));
    }

    public void Approve()
    {
        if (IsApproved)
        {
            throw new DomainException("Đợt audit này đã được duyệt.");
        }

        if (Lines.Count == 0)
        {
            throw new DomainException("Không thể duyệt đợt audit không có dòng đếm.");
        }

        Status = ApprovedStatus;
    }
}
