using BookSalesManagement.Application.Dtos;

namespace BookSalesManagement.Web.ViewModels;

public sealed class BookEntryInput
{
    public string BookId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal CurrentQuantity { get; set; }

    public bool Selected { get; set; }

    public decimal? CountedQuantity { get; set; }
}

public sealed class BookSelectionForm
{
    public List<BookEntryInput> Books { get; set; } = new();
}

public sealed class CountDetailsViewModel
{
    public InventoryCountDetail Count { get; init; } = default!;

    public IReadOnlyList<BookOption> CountBooks { get; init; } = Array.Empty<BookOption>();
}

public sealed class FinalizeCountForm
{
    public string CountId { get; set; } = string.Empty;

    public Dictionary<string, long> Selections { get; set; } = new();
}

public sealed class AddObservationForm
{
    public string CountId { get; set; } = string.Empty;

    public string BookId { get; set; } = string.Empty;

    public decimal? CountedQuantity { get; set; }
}

public sealed class AuditDetailsViewModel
{
    public InventoryAuditDetail Audit { get; init; } = default!;

    public bool CanApprove { get; init; }
}

public sealed record SessionViewModel(IReadOnlyList<DemoAccount> Accounts, string? CurrentAccountId);

public sealed record DashboardViewModel(
    IReadOnlyList<StockOverviewRow> Stock,
    IReadOnlyList<MovementRow> Movements,
    IReadOnlyList<InventoryCountSummary> Counts,
    IReadOnlyList<InventoryAuditSummary> Audits,
    bool CanCount,
    bool CanAudit)
{
    public decimal TotalUnits => Stock.Sum(row => row.CurrentQuantity);

    public int LowStockCount => Stock.Count(row => row.CurrentQuantity <= 2);

    public int LedgerMismatchCount => Stock.Count(row => row.Difference != 0);

    public int OpenCount => Counts.Count(count => !count.IsFinalized);
}

public sealed record PurchaseIndexViewModel(
    IReadOnlyList<PurchaseOrderRow> Orders,
    IReadOnlyList<GoodsReceiptRow> Receipts);
