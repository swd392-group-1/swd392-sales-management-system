using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class BookReturnLineConfiguration : IEntityTypeConfiguration<BookReturnLine>
{
    public void Configure(EntityTypeBuilder<BookReturnLine> builder)
    {
        builder.ToTable("BookReturnLine", table =>
        {
            table.HasCheckConstraint("CK_BookReturnLine_1", "[returned_quantity] > 0");
            table.HasCheckConstraint("CK_BookReturnLine_2", "[refund_amount] >= 0");
        });
        builder.HasKey(line => line.ReturnLineId);

        builder.Property(line => line.ReturnLineId)
            .HasColumnName("return_line_id")
            .HasColumnType("bigint")
            .UseIdentityColumn();
        builder.Property(line => line.ReturnId)
            .HasColumnName("return_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(line => line.OriginalOrderLineId)
            .HasColumnName("original_order_line_id")
            .HasColumnType("bigint")
            .IsRequired();
        builder.Property(line => line.ReturnedQuantity)
            .HasColumnName("returned_quantity")
            .HasColumnType("decimal(38,10)")
            .IsRequired();
        builder.Property(line => line.RefundAmount)
            .HasColumnName("refund_amount")
            .HasColumnType("decimal(19,4)")
            .IsRequired();

        builder.HasOne(line => line.BookReturn)
            .WithMany(bookReturn => bookReturn.Lines)
            .HasForeignKey(line => line.ReturnId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_BookReturnLine_1");
        builder.HasOne(line => line.OriginalOrderLine)
            .WithMany(orderLine => orderLine.ReturnLines)
            .HasForeignKey(line => line.OriginalOrderLineId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_BookReturnLine_2");
    }
}
