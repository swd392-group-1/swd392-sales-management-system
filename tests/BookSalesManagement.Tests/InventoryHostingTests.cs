using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookSalesManagement.Tests;

public sealed class InventoryHostingTests
{
    [Theory]
    [InlineData("/Session")]
    [InlineData("/Counts")]
    [InlineData("/Audits")]
    public async Task DemoAccountAppIsUnavailableInProduction(string path)
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Theory]
    [InlineData("/Session/Select")]
    [InlineData("/Counts/Finalize")]
    public async Task DemoWritesAreUnavailableInProduction(string path)
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync(path, null)).StatusCode);
    }
}
