using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class InventoryQueryService : IInventoryQueryService
{
    private readonly BookSalesDbContext _db;
    private readonly IBookDirectory _books;

    public InventoryQueryService(BookSalesDbContext db, IBookDirectory books)
    {
        _db = db;
        _books = books;
    }

    public async Task<IReadOnlyList<StockOverviewRow>> GetStockOverviewAsync(CancellationToken cancellationToken = default)
    {
        var rows = await (
                from reconciliation in _db.StockReconciliation.AsNoTracking()
                join book in _db.Books.AsNoTracking() on reconciliation.BookId equals book.Id
                orderby book.Title
                select new StockOverviewRow(
                    reconciliation.BookId,
                    book.Title ?? reconciliation.BookId,
                    reconciliation.CurrentQuantity,
                    reconciliation.LedgerQuantity,
                    reconciliation.Difference))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<IReadOnlyList<AdjustmentSummary>> GetAdjustmentsAsync(CancellationToken cancellationToken = default)
    {
        var adjustments = await _db.InventoryAdjustments
            .AsNoTracking()
            .OrderByDescending(adjustment => adjustment.AdjustmentDate)
            .ThenByDescending(adjustment => adjustment.Id)
            .Select(adjustment => new
            {
                adjustment.Id,
                adjustment.AdjustmentDate,
                adjustment.Reason,
                Books = adjustment.Books
                    .Select(book => new { book.BookId, book.QuantityChange })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var links = await _db.CountAdjustments
            .AsNoTracking()
            .ToDictionaryAsync(link => link.AdjustmentId, link => link.CountId, cancellationToken);

        var titles = await _books.GetTitlesAsync(
            adjustments.SelectMany(adjustment => adjustment.Books).Select(book => book.BookId),
            cancellationToken);

        return adjustments
            .Select(adjustment => new AdjustmentSummary(
                adjustment.Id,
                adjustment.AdjustmentDate,
                adjustment.Reason,
                links.GetValueOrDefault(adjustment.Id),
                adjustment.Books
                    .Select(book => new AdjustmentLine(
                        book.BookId,
                        titles.TryGetValue(book.BookId, out var title) ? title : book.BookId,
                        book.QuantityChange))
                    .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<MovementRow>> GetRecentMovementsAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(take, 1, 500);
        var rows = await (
                from movement in _db.InventoryMovements.AsNoTracking()
                join book in _db.Books.AsNoTracking() on movement.BookId equals book.Id
                orderby movement.OccurredAtUtc descending, movement.Id descending
                select new MovementRow(
                    movement.Id,
                    movement.BookId,
                    book.Title ?? movement.BookId,
                    movement.QuantityDelta,
                    movement.OccurredAtUtc,
                    movement.PostingKey))
            .Take(size)
            .ToListAsync(cancellationToken);

        return rows;
    }
}

public sealed class PurchaseQueryService : IPurchaseQueryService
{
    private readonly BookSalesDbContext _db;
    private readonly IBookDirectory _books;

    public PurchaseQueryService(BookSalesDbContext db, IBookDirectory books)
    {
        _db = db;
        _books = books;
    }

    public async Task<IReadOnlyList<PurchaseOrderRow>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _db.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Lines)
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.Id)
            .ToListAsync(cancellationToken);

        var providerLinks = await _db.PurchaseOrderProviders
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var providers = await _db.Providers
            .AsNoTracking()
            .ToDictionaryAsync(provider => provider.Id, provider => provider.Name ?? provider.Id, cancellationToken);

        var receivedByLine = await _db.GoodsReceiptLines
            .AsNoTracking()
            .GroupBy(line => line.PurchaseOrderLineId)
            .Select(group => new { LineId = group.Key, Quantity = group.Sum(line => line.ReceivedQuantity) })
            .ToDictionaryAsync(row => row.LineId, row => row.Quantity, cancellationToken);

        var titles = await _books.GetTitlesAsync(
            orders.SelectMany(order => order.Lines).Select(line => line.BookId),
            cancellationToken);

        return orders
            .Select(order => new PurchaseOrderRow(
                order.Id,
                order.OrderDate,
                order.Status ?? string.Empty,
                providerLinks
                    .Where(link => link.PurchaseOrderId == order.Id)
                    .Select(link => providers.TryGetValue(link.ProviderId, out var name) ? name : link.ProviderId)
                    .ToArray(),
                order.Lines
                    .Select(line => new PurchaseOrderLineRow(
                        line.Id,
                        line.BookId,
                        titles.TryGetValue(line.BookId, out var title) ? title : line.BookId,
                        line.OrderedQuantity,
                        receivedByLine.GetValueOrDefault(line.Id)))
                    .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<GoodsReceiptRow>> GetGoodsReceiptsAsync(CancellationToken cancellationToken = default)
    {
        var receipts = await _db.GoodsReceipts
            .AsNoTracking()
            .Include(receipt => receipt.Lines)
            .OrderByDescending(receipt => receipt.ReceiptDate)
            .ThenByDescending(receipt => receipt.Id)
            .ToListAsync(cancellationToken);

        var orderLines = await _db.PurchaseOrderLines
            .AsNoTracking()
            .Select(line => new { line.Id, line.PurchaseOrderId, line.BookId })
            .ToDictionaryAsync(line => line.Id, cancellationToken);

        var titles = await _books.GetTitlesAsync(
            orderLines.Values.Select(line => line.BookId),
            cancellationToken);

        return receipts
            .Select(receipt => new GoodsReceiptRow(
                receipt.Id,
                receipt.ReceiptDate,
                receipt.Lines
                    .Select(line =>
                    {
                        var orderLine = orderLines.GetValueOrDefault(line.PurchaseOrderLineId);
                        var bookId = orderLine?.BookId ?? string.Empty;
                        return new GoodsReceiptLineRow(
                            orderLine?.PurchaseOrderId ?? string.Empty,
                            bookId,
                            titles.TryGetValue(bookId, out var title) ? title : bookId,
                            line.ReceivedQuantity);
                    })
                    .ToArray()))
            .ToArray();
    }
}
