using BookSalesManagement.Web;
using BookSalesManagement.Web.Infrastructure;
using BookSalesManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("BookSales")
    ?? string.Empty;

builder.Services.AddBookSalesServices(connectionString);
builder.Services.AddScoped<ICurrentUser, DemoCurrentUser>();

var app = builder.Build();

// The demo cookie selects an account without password authentication.
// Keep this separate app local until it uses the shared Login/authorization.
app.Use(async (context, next) =>
{
    if (!app.Environment.IsDevelopment()
        || context.Connection.RemoteIpAddress is not { } ip
        || !System.Net.IPAddress.IsLoopback(ip))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseMiddleware<DemoSessionMiddleware>();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program;
