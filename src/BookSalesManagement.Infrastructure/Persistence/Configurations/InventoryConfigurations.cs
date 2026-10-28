using BookSalesManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookSalesManagement.Infrastructure.Persistence.Configurations;

public sealed class InventoryCountConfiguration : IEntityTypeConfiguration<InventoryCount>
{
    public void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        builder.ToTable("InventoryCount", "dbo");
        builder.HasKey(count => count.Id);
        builder.Property(count => count.Id).HasColumnName("count_id").HasMaxLength(200);
        builder.Property(count => count.CountedAtUtc).HasColumnName("counted_at_utc");
        builder.Property(count => count.RecordedByEmployeeId).HasColumnName("recorded_by_employee_id").HasMaxLength(200);
        builder.Ignore(count => count.IsFinalized);

        builder.HasMany(count => count.Observations)
            .WithOne()
            .HasForeignKey(observation => observation.CountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(count => count.Selections)
            .WithOne()
            .HasForeignKey(selection => selection.CountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryCountObservationConfiguration : IEntityTypeConfiguration<InventoryCountObservation>
{
    public void Configure(EntityTypeBuilder<InventoryCountObservation> builder)
    {
        builder.ToTable("InventoryCountObservation", "dbo");
        builder.HasKey(observation => observation.Id);
        builder.Property(observation => observation.Id).HasColumnName("observation_id").ValueGeneratedOnAdd();
        builder.Property(observation => observation.CountId).HasColumnName("count_id").HasMaxLength(200);
        builder.Property(observation => observation.BookId).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(observation => observation.CountedQuantity).HasColumnName("counted_quantity").HasPrecision(38, 10);
        builder.Property(observation => observation.ObservedAtUtc).HasColumnName("observed_at_utc");
    }
}

public sealed class InventoryCountSelectionConfiguration : IEntityTypeConfiguration<InventoryCountSelection>
{
    public void Configure(EntityTypeBuilder<InventoryCountSelection> builder)
    {
        builder.ToTable("InventoryCountSelection", "dbo");
        builder.HasKey(selection => new { selection.CountId, selection.BookId });
        builder.Property(selection => selection.CountId).HasColumnName("count_id").HasMaxLength(200);
        builder.Property(selection => selection.BookId).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(selection => selection.SelectedObservationId).HasColumnName("selected_observation_id");
        builder.Property(selection => selection.RecordedQuantity).HasColumnName("recorded_quantity").HasPrecision(38, 10);

        builder.HasOne<InventoryCountObservation>()
            .WithMany()
            .HasForeignKey(selection => new { selection.SelectedObservationId, selection.CountId, selection.BookId })
            .HasPrincipalKey(observation => new { observation.Id, observation.CountId, observation.BookId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("InventoryAdjustment", "dbo");
        builder.HasKey(adjustment => adjustment.Id);
        builder.Property(adjustment => adjustment.Id).HasColumnName("adjustment_id").HasMaxLength(200);
        builder.Property(adjustment => adjustment.AdjustmentDate).HasColumnName("adjustment_date");
        builder.Property(adjustment => adjustment.Reason).HasColumnName("reason").HasColumnType("nvarchar(max)");

        builder.HasMany(adjustment => adjustment.Books)
            .WithOne()
            .HasForeignKey(book => book.AdjustmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AdjustmentBookConfiguration : IEntityTypeConfiguration<AdjustmentBook>
{
    public void Configure(EntityTypeBuilder<AdjustmentBook> builder)
    {
        builder.ToTable("AdjustmentBook", "dbo");
        builder.HasKey(book => new { book.AdjustmentId, book.BookId });
        builder.Property(book => book.AdjustmentId).HasColumnName("adjustment_id").HasMaxLength(200);
        builder.Property(book => book.BookId).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(book => book.QuantityChange).HasColumnName("quantity_change").HasPrecision(38, 10);
    }
}

public sealed class CountAdjustmentConfiguration : IEntityTypeConfiguration<CountAdjustment>
{
    public void Configure(EntityTypeBuilder<CountAdjustment> builder)
    {
        builder.ToTable("CountAdjustment", "dbo");
        builder.HasKey(link => link.CountId);
        builder.Property(link => link.CountId).HasColumnName("count_id").HasMaxLength(200);
        builder.Property(link => link.AdjustmentId).HasColumnName("adjustment_id").HasMaxLength(200);
        builder.HasIndex(link => link.AdjustmentId).IsUnique();

        builder.HasOne<InventoryCount>()
            .WithMany()
            .HasForeignKey(link => link.CountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<InventoryAdjustment>()
            .WithMany()
            .HasForeignKey(link => link.AdjustmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovement", "dbo");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Id).HasColumnName("movement_id").ValueGeneratedOnAdd();
        builder.Property(movement => movement.PostingKey).HasColumnName("posting_key").HasMaxLength(200);
        builder.Property(movement => movement.BookId).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(movement => movement.QuantityDelta).HasColumnName("quantity_delta").HasPrecision(38, 10);
        builder.Property(movement => movement.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.HasIndex(movement => movement.PostingKey).IsUnique();
    }
}

public sealed class InventoryAuditConfiguration : IEntityTypeConfiguration<InventoryAudit>
{
    public void Configure(EntityTypeBuilder<InventoryAudit> builder)
    {
        builder.ToTable("InventoryAudit", "dbo");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id).HasColumnName("audit_id").HasMaxLength(200);
        builder.Property(audit => audit.AuditDate).HasColumnName("audit_date");
        builder.Property(audit => audit.Status).HasColumnName("status").HasColumnType("nvarchar(max)");
        builder.Ignore(audit => audit.IsApproved);

        builder.HasMany(audit => audit.Lines)
            .WithOne()
            .HasForeignKey(line => line.AuditId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InventoryAuditLineConfiguration : IEntityTypeConfiguration<InventoryAuditLine>
{
    public void Configure(EntityTypeBuilder<InventoryAuditLine> builder)
    {
        builder.ToTable("InventoryAuditLine", "dbo");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).HasColumnName("audit_line_id").ValueGeneratedOnAdd();
        builder.Property(line => line.AuditId).HasColumnName("audit_id").HasMaxLength(200);
        builder.Property(line => line.BookId).HasColumnName("book_id").HasMaxLength(200);
        builder.Property(line => line.CountedQuantity).HasColumnName("counted_quantity").HasPrecision(38, 10);
    }
}
