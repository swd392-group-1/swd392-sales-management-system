namespace BookSales.Application.Reports;

public sealed record InventoryReportRow(string BookId, string? Title, decimal? CurrentPrice, decimal? Quantity);
public sealed record InventoryReportFilter(string? Search = null, bool InStockOnly = false);
public sealed class InventoryReport(DateTimeOffset generatedAtUtc, IReadOnlyList<InventoryReportRow> rows)
{
    public DateTimeOffset GeneratedAtUtc { get; } = generatedAtUtc;
    public IReadOnlyList<InventoryReportRow> Rows { get; } = Array.AsReadOnly(rows.ToArray());
    public decimal TotalQuantity => Rows.Sum(r => r.Quantity ?? 0);
    public int MissingStockCount => Rows.Count(r => r.Quantity is null);
}
public interface IInventoryReportQuery
{
    Task<IReadOnlyList<InventoryReportRow>> ReadAsync(InventoryReportFilter filter, CancellationToken cancellationToken);
}
public sealed class InventoryReportService(IInventoryReportQuery query, TimeProvider clock)
{
    public async Task<InventoryReport> GenerateAsync(InventoryReportFilter filter, CancellationToken ct)
    {
        var search = filter.Search?.Trim();
        if (search?.Length > 200) throw new ArgumentException("Search is limited to 200 characters.");
        var rows = await query.ReadAsync(filter with { Search = search }, ct);
        return new InventoryReport(clock.GetUtcNow(), rows);
    }
}
