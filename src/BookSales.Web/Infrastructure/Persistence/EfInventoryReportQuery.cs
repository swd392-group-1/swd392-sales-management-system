using BookSales.Application.Reports;
using Microsoft.EntityFrameworkCore;
namespace BookSales.Infrastructure.Persistence;

public sealed class EfInventoryReportQuery(BookSalesDbContext db) : IInventoryReportQuery
{
    public async Task<IReadOnlyList<InventoryReportRow>> ReadAsync(InventoryReportFilter filter, CancellationToken ct)
    {
        var query = db.Books.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(filter.Search))
            query = query.Where(b => b.BookId.Contains(filter.Search) || (b.Title != null && b.Title.Contains(filter.Search)));
        if (filter.InStockOnly) query = query.Where(b => b.Stock != null && b.Stock.CurrentQuantity > 0);
        // One SQL query; missing stock is unknown, never silently reported as zero.
        return await query.OrderBy(b => b.BookId).Select(b => new InventoryReportRow(
            b.BookId, b.Title, b.CurrentPrice, b.Stock == null ? null : b.Stock.CurrentQuantity)).ToListAsync(ct);
    }
}
