using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Infrastructure;
using BookSalesManagement.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("BookSales")
    ?? throw new InvalidOperationException("Thiếu connection string 'BookSales'.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<ICurrentUser, DemoCurrentUser>();

var app = builder.Build();

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
