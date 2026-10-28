using BookSalesManagement.Domain.Catalog;
using BookSalesManagement.Domain.Identity;
using BookSalesManagement.Domain.Inventory;
using BookSalesManagement.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class BookSalesDbContext : DbContext
{
    public BookSalesDbContext(DbContextOptions<BookSalesDbContext> options) : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookStock> BookStocks => Set<BookStock>();

    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();

    public DbSet<InventoryCountObservation> InventoryCountObservations => Set<InventoryCountObservation>();

    public DbSet<InventoryCountSelection> InventoryCountSelections => Set<InventoryCountSelection>();

    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();

    public DbSet<AdjustmentBook> AdjustmentBooks => Set<AdjustmentBook>();

    public DbSet<CountAdjustment> CountAdjustments => Set<CountAdjustment>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public DbSet<InventoryAudit> InventoryAudits => Set<InventoryAudit>();

    public DbSet<InventoryAuditLine> InventoryAuditLines => Set<InventoryAuditLine>();

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<PurchaseOrderProvider> PurchaseOrderProviders => Set<PurchaseOrderProvider>();

    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();

    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<EmployeePermission> EmployeePermissions => Set<EmployeePermission>();

    public DbSet<StockReconciliationView> StockReconciliation => Set<StockReconciliationView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookSalesDbContext).Assembly);
    }
}
