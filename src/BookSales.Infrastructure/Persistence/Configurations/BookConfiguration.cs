using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Book");
        builder.HasKey(book => book.BookId);

        builder.Property(book => book.BookId)
            .HasColumnName("book_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(book => book.Title)
            .HasColumnName("title")
            .HasColumnType("nvarchar(max)");
        builder.Property(book => book.CurrentPrice)
            .HasColumnName("current_price")
            .HasColumnType("decimal(19,4)");
    }
}
