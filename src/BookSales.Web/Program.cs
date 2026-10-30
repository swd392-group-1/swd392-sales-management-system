using BookSales.Application.Notifications;
using BookSales.Application.Services;
using BookSales.Domain.Interfaces;
using BookSales.Infrastructure.Notifications;
using BookSales.Infrastructure.Persistence;
using BookSales.Web.Realtime;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<INoticeQueue>(_ => new ChannelNoticeQueue());
builder.Services.AddScoped<DevelopmentNoticeService>();
builder.Services.AddSingleton<INoticePublisher, SignalRNoticePublisher>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, MockBackgroundTaskQueue>();

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
