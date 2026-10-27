using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookSales.Tests;

public sealed class WebFoundationTests
{
    private static WebApplicationFactory<Program> ProductionApplication() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

    [Fact]
    public async Task FoundationRunsWithoutAConfiguredDatabase()
    {
        await using var app = ProductionApplication();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadAsStringAsync();
        Assert.Contains("net8.0", status);
        Assert.Contains("\"databaseConfigured\":false", status);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Theory]
    [InlineData("/development")]
    [InlineData("/development/database")]
    public async Task DevelopmentPagesAreNotExposedInProduction(string path)
    {
        await using var app = ProductionApplication();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task ProductionCannotEnqueueOrNegotiateDevelopmentNotifications()
    {
        await using var app = ProductionApplication();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/development/notices", new { message = "not accepted" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/hubs/development/negotiate?negotiateVersion=1", null)).StatusCode);
    }
}
