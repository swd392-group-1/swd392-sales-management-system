using BookSales.Domain.Access;
using BookSales.Domain.Entities;
using BookSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Tests;

public sealed class MergedModelTests
{
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
