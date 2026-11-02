using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
{
    public void Configure(EntityTypeBuilder<OrderPayment> builder)
    {
        builder.ToTable("OrderPayment");
        builder.HasKey(orderPayment => new { orderPayment.OrderId, orderPayment.PaymentId });

        builder.Property(orderPayment => orderPayment.OrderId)
            .HasColumnName("order_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(orderPayment => orderPayment.PaymentId)
            .HasColumnName("payment_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(orderPayment => orderPayment.SalesOrder)
            .WithMany(order => order.OrderPayments)
            .HasForeignKey(orderPayment => orderPayment.OrderId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_OrderPayment_1");
        builder.HasOne(orderPayment => orderPayment.Payment)
            .WithMany(payment => payment.OrderPayments)
            .HasForeignKey(orderPayment => orderPayment.PaymentId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_OrderPayment_2");
    }
}
