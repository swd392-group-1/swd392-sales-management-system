using BookSales.Domain.Entities;
using BookSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BookSales.Tests;

public sealed class BookReturnMappingTests
{
    [Fact]
    public void ReturnOrderLockQuery_PreservesSqlServerLockHintsWhenIncludingRelatedData()
    {
        var options = new DbContextOptionsBuilder<BookSalesDbContext>()
            .UseSqlServer("Server=(local);Database=BookSalesModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new BookSalesDbContext(options);

        var orderId = "lock-query-test";
        var sql = db.SalesOrders
            .FromSqlInterpolated($"SELECT * FROM dbo.SalesOrder WITH (UPDLOCK, HOLDLOCK) WHERE order_id = {orderId}")
            .Include(order => order.Lines)
                .ThenInclude(line => line.Book)
            .Include(order => order.OrderPayments)
                .ThenInclude(orderPayment => orderPayment.Payment)
            .ToQueryString();

        Assert.Contains("UPDLOCK, HOLDLOCK", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dbo.SalesOrder", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReturnEntities_MapOnlyTheExistingColumnsAndConstraints()
    {
        var options = new DbContextOptionsBuilder<BookSalesDbContext>()
            .UseSqlServer("Server=(local);Database=BookSalesModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new BookSalesDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var returnEntity = model.FindEntityType(typeof(BookReturn));
        var returnLineEntity = model.FindEntityType(typeof(BookReturnLine));
        Assert.NotNull(returnEntity);
        Assert.NotNull(returnLineEntity);

        var returnTable = StoreObjectIdentifier.Table(
            returnEntity!.GetTableName()!,
            returnEntity.GetSchema());
        var returnLineTable = StoreObjectIdentifier.Table(
            returnLineEntity!.GetTableName()!,
            returnLineEntity.GetSchema());

        Assert.Equal("BookReturn", returnEntity.GetTableName());
        Assert.Equal("BookReturnLine", returnLineEntity.GetTableName());
        Assert.Equal("return_id", returnEntity.FindProperty(nameof(BookReturn.ReturnId))!.GetColumnName(returnTable));
        Assert.Equal("original_order_id", returnEntity.FindProperty(nameof(BookReturn.OriginalOrderId))!.GetColumnName(returnTable));
        Assert.Equal("returned_at_utc", returnEntity.FindProperty(nameof(BookReturn.ReturnedAtUtc))!.GetColumnName(returnTable));
        Assert.Equal("return_line_id", returnLineEntity.FindProperty(nameof(BookReturnLine.ReturnLineId))!.GetColumnName(returnLineTable));
        Assert.Equal("return_id", returnLineEntity.FindProperty(nameof(BookReturnLine.ReturnId))!.GetColumnName(returnLineTable));
        Assert.Equal("original_order_line_id", returnLineEntity.FindProperty(nameof(BookReturnLine.OriginalOrderLineId))!.GetColumnName(returnLineTable));
        Assert.Equal("returned_quantity", returnLineEntity.FindProperty(nameof(BookReturnLine.ReturnedQuantity))!.GetColumnName(returnLineTable));
        Assert.Equal("refund_amount", returnLineEntity.FindProperty(nameof(BookReturnLine.RefundAmount))!.GetColumnName(returnLineTable));
        Assert.Equal("nvarchar(200)", returnEntity.FindProperty(nameof(BookReturn.ReturnId))!.GetColumnType());
        Assert.Equal("datetime2(7)", returnEntity.FindProperty(nameof(BookReturn.ReturnedAtUtc))!.GetColumnType());
        Assert.Equal("decimal(38,10)", returnLineEntity.FindProperty(nameof(BookReturnLine.ReturnedQuantity))!.GetColumnType());
        Assert.Equal("decimal(19,4)", returnLineEntity.FindProperty(nameof(BookReturnLine.RefundAmount))!.GetColumnType());
        Assert.Equal(
            new[] { "CK_BookReturnLine_1", "CK_BookReturnLine_2" },
            returnLineEntity.GetCheckConstraints().Select(constraint => constraint.Name).OrderBy(name => name).ToArray());
    }

    [Fact]
    public void PaymentAndInventoryMovement_DoNotMapInventedColumns()
    {
        var options = new DbContextOptionsBuilder<BookSalesDbContext>()
            .UseSqlServer("Server=(local);Database=BookSalesModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new BookSalesDbContext(options);

        var model = db.GetService<IDesignTimeModel>().Model;
        var payment = model.FindEntityType(typeof(Payment));
        var movement = model.FindEntityType(typeof(InventoryMovement));

        Assert.NotNull(payment);
        Assert.NotNull(movement);
        Assert.Null(payment!.FindProperty("Amount"));
        Assert.Null(movement!.FindProperty("ReturnId"));
        Assert.Null(movement.FindProperty("OrderId"));
    }
}
