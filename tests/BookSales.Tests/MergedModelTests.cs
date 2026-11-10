using BookSales.Domain.Access;
using BookSales.Domain.Entities;
using BookSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Tests;

public sealed class MergedModelTests
{
    [Fact]
    public void ContextsInOneAssemblyKeepTheirOwnMappings()
    {
        using var sales = new BookSalesDbContext(new DbContextOptionsBuilder<BookSalesDbContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Integrated Security=True").Options);
        using var inventory = new BookSalesManagement.Web.Data.BookSalesDbContext(
            new DbContextOptionsBuilder<BookSalesManagement.Web.Data.BookSalesDbContext>()
                .UseSqlServer("Server=(local);Database=ModelOnly;Integrated Security=True").Options);

        Assert.NotNull(inventory.Model.FindEntityType(
            typeof(BookSalesManagement.Web.Models.Inventory.InventoryCount)));
        Assert.Null(inventory.Model.FindEntityType(typeof(Book)));
        Assert.Null(sales.Model.FindEntityType(typeof(BookSalesManagement.Web.Models.Catalog.Book)));
        Assert.Contains("[InventoryCount]", inventory.InventoryCounts.ToQueryString());
        Assert.Contains("[Book]", sales.Books.ToQueryString());
    }

    [Fact]
    public void AccessAndExistingBusinessMappingsCoexistInTheSameContext()
    {
        using var db = new BookSalesDbContext(new DbContextOptionsBuilder<BookSalesDbContext>()
            .UseSqlServer("Server=(local);Database=ModelOnly;Integrated Security=True")
            .Options);

        foreach (var type in new[] { typeof(Account), typeof(Employee), typeof(Permission),
            typeof(EmployeePermission), typeof(UserAccessLog), typeof(Book), typeof(BookStock),
            typeof(SalesOrder), typeof(SalesOrderLine), typeof(Payment), typeof(OrderPayment),
            typeof(BookReturn), typeof(BookReturnLine), typeof(InventoryMovement) })
        {
            Assert.NotNull(db.Model.FindEntityType(type));
        }
        Assert.Equal(512, db.Model.FindEntityType(typeof(Account))!
            .FindProperty(nameof(Account.PasswordHash))!.GetMaxLength());
        Assert.Contains("[dbo].[Account]", db.Accounts.ToQueryString());
        Assert.Contains("[BookReturn]", db.BookReturns.ToQueryString());
    }
}
