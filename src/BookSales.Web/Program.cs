using BookSales.Web.Middleware;
using BookSales.Application.Access;
using BookSales.Application.Notifications;
using BookSales.Infrastructure.Access;
using BookSales.Infrastructure.Notifications;
using BookSales.Infrastructure.Persistence;
using BookSales.Web.Realtime;
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
builder.Services.AddSingleton<IPasswordHashVerifier,UnconfiguredPasswordHashVerifier>();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<INoticeQueue>(_ => new ChannelNoticeQueue());
builder.Services.AddScoped<DevelopmentNoticeService>();
builder.Services.AddSingleton<INoticePublisher, SignalRNoticePublisher>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<NoticeDispatchWorker>();
}

var connectionString = builder.Configuration.GetConnectionString("BookSales");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<BookSalesDbContext>(options => options.UseSqlServer(connectionString));
}

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
if (app.Environment.IsDevelopment())
{
    app.MapHub<DevelopmentNoticeHub>("/hubs/development");
}
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
