using BookSales.Domain.Access;
using BookSales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Persistence;

public sealed class BookSalesDbContext(
    DbContextOptions<BookSalesDbContext> options
) : DbContext(options)
{
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

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<EmployeePermission> EmployeePermissions =>
        Set<EmployeePermission>();
    public DbSet<UserAccessLog> UserAccessLogs => Set<UserAccessLog>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookSalesDbContext).Assembly);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee", "dbo");
            entity.HasKey(e => e.EmployeeId);

            entity.Property(e => e.EmployeeId)
                .HasColumnName("employee_id")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.RoleCode)
                .HasColumnName("role_code")
                .HasMaxLength(32)
                .IsRequired();
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Account", "dbo");
            entity.HasKey(a => a.AccountId);

            entity.Property(a => a.AccountId)
                .HasColumnName("account_id")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.EmployeeId)
                .HasColumnName("employee_id")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.Username)
                .HasColumnName("username")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(a => a.Status)
                .HasColumnName("status")
                .HasColumnType("nvarchar(max)");

            entity.Property(a => a.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(512);

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_1");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permission", "dbo");
            entity.HasKey(p => p.PermissionId);

            entity.Property(p => p.PermissionId)
                .HasColumnName("permission_id")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(p => p.PermissionName)
                .HasColumnName("permission_name")
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<EmployeePermission>(entity =>
        {
            entity.ToTable("EmployeePermission", "dbo");

            entity.HasKey(ep => new
            {
                ep.EmployeeId,
                ep.PermissionId
            });

            entity.Property(ep => ep.EmployeeId)
                .HasColumnName("employee_id")
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(ep => ep.PermissionId)
                .HasColumnName("permission_id")
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(ep => ep.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_EmployeePermission_1");

            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(ep => ep.PermissionId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_EmployeePermission_2");
        });

        modelBuilder.Entity<UserAccessLog>(entity =>
        {
            entity.ToTable("UserAccessLog", "dbo");
            entity.HasKey(l => l.AccessLogId);

            entity.Property(l => l.AccessLogId)
                .HasColumnName("access_log_id")
                .UseIdentityColumn(1, 1);

            entity.Property(l => l.AccountId)
                .HasColumnName("account_id")
                .HasMaxLength(200);

            entity.Property(l => l.OccurredAtUtc)
                .HasColumnName("occurred_at_utc")
                .HasColumnType("datetime2(7)")
                .IsRequired();

            entity.Property(l => l.RequestPath)
                .HasColumnName("request_path")
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(l => l.WasAuthorized)
                .HasColumnName("was_authorized")
                .IsRequired();

            entity.Property(l => l.ClientIp)
                .HasColumnName("client_ip")
                .HasMaxLength(45)
                .IsRequired();

            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_UserAccessLog_1");
        });
    }
}
