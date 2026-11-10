using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payment");
        builder.HasKey(payment => payment.PaymentId);

        builder.Property(payment => payment.PaymentId)
            .HasColumnName("payment_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(payment => payment.PaymentMethod)
            .HasColumnName("payment_method")
            .HasColumnType("nvarchar(max)");
        builder.Property(payment => payment.PaymentStatus)
            .HasColumnName("payment_status")
            .HasColumnType("nvarchar(max)");
    }
}
