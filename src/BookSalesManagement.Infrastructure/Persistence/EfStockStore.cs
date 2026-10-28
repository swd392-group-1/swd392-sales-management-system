using BookSalesManagement.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class EfStockStore : IStockStore
{
    private readonly BookSalesDbContext _db;

    public EfStockStore(BookSalesDbContext db)
    {
        _db = db;
    }

    public async Task EnsureStockRowsAsync(
        IReadOnlyCollection<string> bookIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var bookId in bookIds.Distinct())
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"""
                IF NOT EXISTS (SELECT 1 FROM dbo.BookStock WHERE book_id = {bookId})
                BEGIN
                    INSERT INTO dbo.BookStock (book_id, current_quantity) VALUES ({bookId}, 0);
                END
                """, cancellationToken);
        }
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetQuantitiesAsync(
        IReadOnlyCollection<string> bookIds,
        CancellationToken cancellationToken = default)
    {
        var ids = bookIds.Distinct().ToArray();
        var quantities = await _db.BookStocks
            .AsNoTracking()
            .Where(stock => ids.Contains(stock.BookId))
            .ToDictionaryAsync(stock => stock.BookId, stock => stock.CurrentQuantity, cancellationToken);

        foreach (var bookId in ids)
        {
            quantities.TryAdd(bookId, 0);
        }

        return quantities;
    }

    public async Task<bool> TrySetQuantityAsync(
        string bookId,
        decimal expectedQuantity,
        decimal newQuantity,
        CancellationToken cancellationToken = default)
    {
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE dbo.BookStock
            SET current_quantity = {newQuantity}
            WHERE book_id = {bookId} AND current_quantity = {expectedQuantity}
            """, cancellationToken);

        return affected == 1;
    }
}
