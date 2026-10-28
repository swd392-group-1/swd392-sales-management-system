namespace BookSalesManagement.Application.Dtos;

public sealed record InventoryAuditSummary(
    string Id,
    DateOnly? AuditDate,
    string Status,
    int LineCount);

public sealed record AuditLineDetail(
    long Id,
    string BookId,
    string BookTitle,
    decimal CountedQuantity,
    decimal CurrentQuantity);

public sealed record InventoryAuditDetail(
    string Id,
    DateOnly? AuditDate,
    string Status,
    bool IsApproved,
    IReadOnlyList<AuditLineDetail> Lines);
