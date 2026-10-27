using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Persistence;

/// <summary>Connection boundary only. Business entity mappings await schema reconciliation.</summary>
public sealed class BookSalesDbContext(DbContextOptions<BookSalesDbContext> options) : DbContext(options);
