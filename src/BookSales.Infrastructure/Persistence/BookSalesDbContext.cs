using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Persistence;

public sealed class BookSalesDbContext : DbContext
{
    public BookSalesDbContext(DbContextOptions<BookSalesDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookStock> BookStocks => Set<BookStock>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<BookReturn> BookReturns => Set<BookReturn>();
    public DbSet<BookReturnLine> BookReturnLines => Set<BookReturnLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookSalesDbContext).Assembly);
    }
}
