using System.Net;
using BookSalesManagement.Web.Models.Results;
using BookSalesManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookSales.Tests;

public sealed class UnifiedInventoryRoutesTests
{
    private static WebApplicationFactory<Program> Application() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountDirectory>();
                services.AddSingleton<IAccountDirectory, Accounts>();
                services.AddTransient<IStartupFilter, LoopbackRequests>();
            });
        });

    [Fact]
    public async Task MainHomeAndInventoryViewsShareOneHostAndKeepAreaLinks()
    {
        await using var app = Application();
        using var client = app.CreateClient();
        var home = await client.GetStringAsync("/");
        Assert.Contains("href=\"/inventory\"", home);

        var inventory = await client.GetStringAsync("/inventory/Session");
        Assert.Contains("action=\"/inventory/Session/Reset\"", inventory);
        Assert.Contains("/inventory-assets/css/site.css", inventory);
        Assert.Contains("name=\"__RequestVerificationToken\"", inventory);
    }

    [Fact]
    public async Task InventoryPostStillRequiresAntiforgeryAfterMerge()
    {
        await using var app = Application();
        using var client = app.CreateClient();
        var response = await client.PostAsync("/inventory/Session/Select",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["accountId"] = "test" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class Accounts : IAccountDirectory
    {
        private static readonly DemoAccount Account = new("test", "employee", "inventory",
            "Inventory Test", "INVENTORY_STAFF", "ACTIVE", true);
        public Task<DemoAccount?> FindAsync(string id, CancellationToken ct = default) =>
            Task.FromResult<DemoAccount?>(Account);
        public Task<IReadOnlyList<DemoAccount>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DemoAccount>>(new[] { Account });
    }

    private sealed class LoopbackRequests : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, following) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                await following(context);
            });
            next(app);
        };
    }
}
