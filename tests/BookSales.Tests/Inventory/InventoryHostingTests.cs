using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookSalesManagement.Tests;

public sealed class InventoryHostingTests
{
    [Theory]
    [InlineData("/inventory/Session")]
    [InlineData("/inventory/Counts")]
    [InlineData("/inventory/Audits")]
    public async Task DemoAccountAppIsUnavailableInProduction(string path)
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("/inventory/Session/Select")]
    [InlineData("/inventory/Counts/Finalize")]
    public async Task DemoWritesAreUnavailableInProduction(string path)
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync(path, null)).StatusCode);
    }
}
