using BookSalesManagement.Web.Data;
using BookSalesManagement.Web.Services;
using BookSalesManagement.Web.Services.Abstractions;
using BookSalesManagement.Web.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Web;

public static class ServiceRegistration
{
    public static IServiceCollection AddBookSalesServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BookSalesDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IInventoryCountStore, EfInventoryCountStore>();
        services.AddScoped<IStockStore, EfStockStore>();
        services.AddScoped<IInventoryAdjustmentStore, EfInventoryAdjustmentStore>();
        services.AddScoped<IInventoryMovementStore, EfInventoryMovementStore>();
        services.AddScoped<IInventoryAuditStore, EfInventoryAuditStore>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IBookDirectory, BookDirectory>();
        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();
        services.AddScoped<IAccountDirectory, AccountDirectory>();
        services.AddScoped<IPermissionChecker, DbPermissionChecker>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IPurchaseQueryService, PurchaseQueryService>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<InventoryCountService>();
        services.AddScoped<InventoryAuditService>();

        return services;
    }
}
