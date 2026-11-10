using System.Security.Claims;
using BookSales.Application.Access;
using BookSales.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace BookSales.Tests;

public sealed class AccessAuditMiddlewareTests
{
    [Theory]
    [InlineData("/access/permissions")]
    [InlineData("/access/demo")]
    [InlineData("/access/logs")]
    [InlineData("/access/logout")]
    public async Task AnonymousProtectedRequest_IsRecordedAsDenied(string path)
    {
        var writer = new RecordingWriter();
        var nextWasCalled = false;
        var middleware = new AccessAuditMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

        await middleware.InvokeAsync(context, writer);

        Assert.True(nextWasCalled);
        Assert.Single(writer.Entries);
        Assert.Null(writer.Entries[0].AccountId);
        Assert.Equal(path, writer.Entries[0].Path);
        Assert.False(writer.Entries[0].Allowed);
        Assert.Equal("127.0.0.1", writer.Entries[0].Ip);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/access/login")]
    [InlineData("/access/denied")]
    public async Task PublicRequest_IsNotLoggedByMiddleware(string path)
    {
        var writer = new RecordingWriter();
        var middleware = new AccessAuditMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        await middleware.InvokeAsync(context, writer);

        Assert.Empty(writer.Entries);
    }

    [Fact]
    public async Task AuthenticatedRequest_IsLeftForControllerAuthorizationAndAudit()
    {
        var writer = new RecordingWriter();
        var middleware = new AccessAuditMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/access/permissions";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "ACC-001")], "Cookies"));

        await middleware.InvokeAsync(context, writer);

        Assert.Empty(writer.Entries);
    }

    private sealed class RecordingWriter : IAccessLogWriter
    {
        public List<(string? AccountId, string Path, bool Allowed, string Ip)> Entries { get; } = [];

        public Task WriteAsync(string? accountId, string requestPath,
            bool wasAuthorized, string clientIp,
            CancellationToken cancellationToken = default)
        {
            Entries.Add((accountId, requestPath, wasAuthorized, clientIp));
            return Task.CompletedTask;
        }
    }
}
