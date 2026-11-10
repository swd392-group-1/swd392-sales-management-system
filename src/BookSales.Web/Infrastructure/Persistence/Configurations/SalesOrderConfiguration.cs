using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrder");
        builder.HasKey(order => order.OrderId);

        builder.Property(order => order.OrderId)
            .HasColumnName("order_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(order => order.OrderDate)
            .HasColumnName("order_date")
            .HasColumnType("date");
        builder.Property(order => order.TotalOrderAmount)
            .HasColumnName("total_order_amount")
            .HasColumnType("decimal(19,4)");
        builder.Property(order => order.CustomerId)
            .HasColumnName("customer_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200);

        builder.HasOne(order => order.Customer)
            .WithMany(customer => customer.SalesOrders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_SalesOrder_1");
    }
}
