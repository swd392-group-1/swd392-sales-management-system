using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class BookStockConfiguration : IEntityTypeConfiguration<BookStock>
{
    public void Configure(EntityTypeBuilder<BookStock> builder)
    {
        builder.ToTable("BookStock");
        builder.HasKey(stock => stock.BookId);

        builder.Property(stock => stock.BookId)
            .HasColumnName("book_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(stock => stock.CurrentQuantity)
            .HasColumnName("current_quantity")
            .HasColumnType("decimal(38,10)")
            .IsRequired();

        builder.HasOne(stock => stock.Book)
            .WithOne(book => book.Stock)
            .HasForeignKey<BookStock>(stock => stock.BookId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_BookStock_1");
    }
}
