using BookSalesManagement.Domain.Catalog;
using BookSalesManagement.Domain.Inventory;
using BookSalesManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesManagement.Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Book", "dbo");
        builder.HasKey(book => book.Id);
        builder.Property(book => book.Id).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(book => book.Title).HasColumnName("title").HasColumnType("nvarchar(max)");
        builder.Property(book => book.CurrentPrice).HasColumnName("current_price").HasPrecision(19, 4);
    }
}

public sealed class BookStockConfiguration : IEntityTypeConfiguration<BookStock>
{
    public void Configure(EntityTypeBuilder<BookStock> builder)
    {
        builder.ToTable("BookStock", "dbo");
        builder.HasKey(stock => stock.BookId);
        builder.Property(stock => stock.BookId).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(stock => stock.CurrentQuantity).HasColumnName("current_quantity").HasPrecision(38, 10);
    }
}

public sealed class StockReconciliationViewConfiguration : IEntityTypeConfiguration<StockReconciliationView>
{
    public void Configure(EntityTypeBuilder<StockReconciliationView> builder)
    {
        builder.HasNoKey();
        builder.ToView("vStockReconciliation", "dbo");
        builder.Property(row => row.BookId).HasColumnName("book_id");
        builder.Property(row => row.CurrentQuantity).HasColumnName("current_quantity").HasPrecision(38, 10);
        builder.Property(row => row.LedgerQuantity).HasColumnName("ledger_quantity").HasPrecision(38, 10);
        builder.Property(row => row.Difference).HasColumnName("difference").HasPrecision(38, 10);
    }
}
