using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookSalesManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<Persistence.BookSalesDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IInventoryCountStore, Persistence.EfInventoryCountStore>();
        services.AddScoped<IStockStore, Persistence.EfStockStore>();
        services.AddScoped<IInventoryAdjustmentStore, Persistence.EfInventoryAdjustmentStore>();
        services.AddScoped<IInventoryMovementStore, Persistence.EfInventoryMovementStore>();
        services.AddScoped<IInventoryAuditStore, Persistence.EfInventoryAuditStore>();
        services.AddScoped<IUnitOfWork, Persistence.EfUnitOfWork>();
        services.AddScoped<IBookDirectory, Persistence.BookDirectory>();
        services.AddScoped<IEmployeeDirectory, Persistence.EmployeeDirectory>();
        services.AddScoped<IAccountDirectory, Persistence.AccountDirectory>();
        services.AddScoped<IPermissionChecker, Persistence.DbPermissionChecker>();
        services.AddScoped<IInventoryQueryService, Persistence.InventoryQueryService>();
        services.AddScoped<IPurchaseQueryService, Persistence.PurchaseQueryService>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<InventoryCountService>();
        services.AddScoped<InventoryAuditService>();

        return services;
    }
}
