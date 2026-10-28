using BookSalesManagement.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesManagement.Infrastructure.Persistence.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("Provider", "dbo");
        builder.HasKey(provider => provider.Id);
        builder.Property(provider => provider.Id).HasColumnName("provider_id").HasMaxLength(200);
        builder.Property(provider => provider.Name).HasColumnName("name").HasColumnType("nvarchar(max)");
    }
}

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrder", "dbo");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).HasColumnName("purchase_order_id").HasMaxLength(200);
        builder.Property(order => order.OrderDate).HasColumnName("order_date");
        builder.Property(order => order.Status).HasColumnName("status").HasColumnType("nvarchar(max)");

        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => line.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLine", "dbo");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).HasColumnName("purchase_order_line_id").ValueGeneratedOnAdd();
        builder.Property(line => line.PurchaseOrderId).HasColumnName("purchase_order_id").HasMaxLength(200);
        builder.Property(line => line.BookId).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(line => line.OrderedQuantity).HasColumnName("ordered_quantity").HasPrecision(38, 10);
    }
}

public sealed class PurchaseOrderProviderConfiguration : IEntityTypeConfiguration<PurchaseOrderProvider>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderProvider> builder)
    {
        builder.ToTable("PurchaseOrderProvider", "dbo");
        builder.HasKey(link => new { link.PurchaseOrderId, link.ProviderId });
        builder.Property(link => link.PurchaseOrderId).HasColumnName("purchase_order_id").HasMaxLength(200);
        builder.Property(link => link.ProviderId).HasColumnName("provider_id").HasMaxLength(200);
    }
}

public sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("GoodsReceipt", "dbo");
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Id).HasColumnName("receipt_id").HasMaxLength(200);
        builder.Property(receipt => receipt.ReceiptDate).HasColumnName("receipt_date");

        builder.HasMany(receipt => receipt.Lines)
            .WithOne()
            .HasForeignKey(line => line.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("GoodsReceiptLine", "dbo");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).HasColumnName("receipt_line_id").ValueGeneratedOnAdd();
        builder.Property(line => line.ReceiptId).HasColumnName("receipt_id").HasMaxLength(200);
        builder.Property(line => line.PurchaseOrderLineId).HasColumnName("purchase_order_line_id");
        builder.Property(line => line.ReceivedQuantity).HasColumnName("received_quantity").HasPrecision(38, 10);
    }
}
