using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLine", table => table.HasCheckConstraint("CK_SalesOrderLine_1", "[quantity] > 0"));
        builder.HasKey(line => line.OrderLineId);

        builder.Property(line => line.OrderLineId)
            .HasColumnName("order_line_id")
            .HasColumnType("bigint")
            .UseIdentityColumn();
        builder.Property(line => line.OrderId)
            .HasColumnName("order_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(line => line.BookId)
            .HasColumnName("book_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(line => line.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("decimal(38,10)")
            .IsRequired();
        builder.Property(line => line.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("decimal(19,4)");

        builder.HasOne(line => line.SalesOrder)
            .WithMany(order => order.Lines)
            .HasForeignKey(line => line.OrderId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_SalesOrderLine_1");
        builder.HasOne(line => line.Book)
            .WithMany(book => book.SalesOrderLines)
            .HasForeignKey(line => line.BookId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_SalesOrderLine_2");
    }
}
