using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSales.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovement");
        builder.HasKey(movement => movement.MovementId);

        builder.Property(movement => movement.MovementId)
            .HasColumnName("movement_id")
            .HasColumnType("bigint")
            .UseIdentityColumn();
        builder.Property(movement => movement.PostingKey)
            .HasColumnName("posting_key")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(movement => movement.BookId)
            .HasColumnName("book_id")
            .HasColumnType("nvarchar(200)")
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(movement => movement.QuantityDelta)
            .HasColumnName("quantity_delta")
            .HasColumnType("decimal(38,10)")
            .IsRequired();
        builder.Property(movement => movement.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasColumnType("datetime2(7)")
            .IsRequired();

        builder.HasAlternateKey(movement => movement.PostingKey)
            .HasName("UQ_InventoryMovement_1");
        builder.HasOne(movement => movement.Book)
            .WithMany(book => book.InventoryMovements)
            .HasForeignKey(movement => movement.BookId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_InventoryMovement_1");
    }
}
