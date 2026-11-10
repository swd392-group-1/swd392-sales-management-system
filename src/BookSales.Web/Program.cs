using BookSales.Application.Reports;
using BookSales.Application.Services;
using BookSales.Domain.Interfaces;
using BookSales.Application.Notifications;
using BookSales.Application.Returns;
using BookSales.Application.Sales;
using BookSales.Web.Access;
using BookSales.Web.Middleware;
using BookSales.Application.Access;
using BookSales.Infrastructure.Access;
using BookSales.Infrastructure.Notifications;
using BookSales.Infrastructure.Persistence;
using BookSales.Infrastructure.Persistence.Returns;
using BookSales.Infrastructure.Persistence.Sales;
using BookSales.Web.Realtime;
using BookSales.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/access/login";
    options.AccessDeniedPath = "/access/denied";
    options.Events.OnValidatePrincipal = async context =>
    {
        var id = context.Principal?.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var db = context.HttpContext.RequestServices
            .GetRequiredService<BookSalesDbContext>();

        var active = id is not null &&
            await db.Accounts.AsNoTracking().AnyAsync(
                a => a.AccountId == id && a.Status == "ACTIVE");

        if (!active)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };
});
builder.Services.AddScoped<IPermissionChecker,SqlPermissionChecker>();
builder.Services.AddScoped<IAccessLogWriter,SqlAccessLogWriter>();
builder.Services.AddScoped<IAccessGuard,AccessGuard>();
builder.Services.AddScoped<IStaffPermissionChangePolicy,SqlStaffPermissionChangePolicy>();
builder.Services.AddScoped<IStaffPermissionStore,SqlStaffPermissionStore>();
builder.Services.AddScoped<StaffPermissionService>();
builder.Services.AddScoped<IAccountLoginService,SqlAccountLoginService>();
builder.Services.AddSingleton<IPasswordHashVerifier, IdentityPasswordHashVerifier>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<QrCodeService>();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IInventoryReportQuery, EfInventoryReportQuery>();
builder.Services.AddScoped<InventoryReportService>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddSingleton<PriceChangeQueue>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(sp => sp.GetRequiredService<PriceChangeQueue>());
builder.Services.AddSingleton<INoticeQueue>(_ => new ChannelNoticeQueue());
builder.Services.AddScoped<DevelopmentNoticeService>();
builder.Services.AddScoped<ISaleProcessingService, SaleProcessingService>();
builder.Services.AddScoped<ISaleRepository, EfSaleRepository>();
builder.Services.AddScoped<IBookReturnService, BookReturnService>();
builder.Services.AddScoped<IBookReturnRepository, EfBookReturnRepository>();
builder.Services.AddSingleton<INoticePublisher, SignalRNoticePublisher>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<NoticeDispatchWorker>();
    builder.Services.AddHostedService<PriceDispatchWorker>();
}

var connectionString = builder.Configuration.GetConnectionString("BookSales");
builder.Services.AddDbContext<BookSalesDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlServer(connectionString);
    }
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.Use(async (context, next) =>
{
    var developmentPath = context.Request.Path.StartsWithSegments("/development")
        || context.Request.Path.StartsWithSegments("/hubs/development");
    var address = context.Connection.RemoteIpAddress;
    if (developmentPath && (!app.Environment.IsDevelopment()
        || address is null || !System.Net.IPAddress.IsLoopback(address)))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});
// Business modules remain local Development demos until the access module is integrated.
app.Use(async (context, next) =>
{
    var paths = new[] { "/reports", "/pricing", "/api/book", "/sales", "/returns", "/hubs/prices" };
    if (paths.Any(p => context.Request.Path.StartsWithSegments(p)) &&
        (!app.Environment.IsDevelopment() || context.Connection.RemoteIpAddress is not { } ip || !System.Net.IPAddress.IsLoopback(ip)))
    { context.Response.StatusCode = 404; return; }
    await next(context);
});
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<AccessAuditMiddleware>();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new
{
    application = "BookSales",
    runtime = "net8.0",
    status = "running",
    databaseConfigured = !string.IsNullOrWhiteSpace(connectionString)
}));
app.MapGet("/health/db", async (BookSalesDbContext db) =>
{
    var connected = await db.Database.CanConnectAsync();

    return Results.Ok(new
    {
        databaseConnected = connected
    });
});
if (app.Environment.IsDevelopment())
{
    app.MapHub<DevelopmentNoticeHub>("/hubs/development");
    app.MapHub<PriceHub>("/hubs/prices");
}
app.MapControllers();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
