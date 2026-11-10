using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class BookReturnConfiguration : IEntityTypeConfiguration<BookReturn>
{
    public void Configure(EntityTypeBuilder<BookReturn> builder)
    {
        builder.ToTable("BookReturn");
        builder.HasKey(bookReturn => bookReturn.ReturnId);

        builder.Property(bookReturn => bookReturn.ReturnId)
            .HasColumnName("return_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(bookReturn => bookReturn.OriginalOrderId)
            .HasColumnName("original_order_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(bookReturn => bookReturn.ReturnedAtUtc)
            .HasColumnName("returned_at_utc")
            .HasColumnType("datetime2(7)")
            .IsRequired();

        builder.HasOne(bookReturn => bookReturn.OriginalOrder)
            .WithMany(order => order.Returns)
            .HasForeignKey(bookReturn => bookReturn.OriginalOrderId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_BookReturn_1");
    }
}
