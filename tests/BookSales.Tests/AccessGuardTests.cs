
using BookSales.Application.Access;
using BookSales.Infrastructure.Access;

namespace BookSales.Tests;

public sealed class AccessGuardTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthorizeAsync_LogsPermissionDecision(
        bool permissionGranted)
    {
        var checker = new FakePermissionChecker(permissionGranted);
        var writer = new FakeAccessLogWriter();
        var guard = new AccessGuard(checker, writer);

        var result = await guard.AuthorizeAsync(
            "ACC-001",
            "UC12",
            "/access/demo",
            "127.0.0.1");

        Assert.Equal(permissionGranted, result);

        Assert.Equal(1, checker.CallCount);
        Assert.Equal("ACC-001", checker.LastAccountId);
        Assert.Equal("UC12", checker.LastPermissionId);

        Assert.Equal(1, writer.CallCount);
        Assert.Equal("ACC-001", writer.AccountId);
        Assert.Equal("/access/demo", writer.RequestPath);
        Assert.Equal("127.0.0.1", writer.ClientIp);
        Assert.Equal(permissionGranted, writer.WasAuthorized);
    }

    [Fact]
    public async Task AuthorizeAsync_WithoutAccount_DeniesAndLogs()
    {
        var checker = new FakePermissionChecker(true);
        var writer = new FakeAccessLogWriter();
        var guard = new AccessGuard(checker, writer);

        var result = await guard.AuthorizeAsync(
            null,
            "UC12",
            "/access/demo",
            "127.0.0.1");

        Assert.False(result);
        Assert.Equal(0, checker.CallCount);

        Assert.Equal(1, writer.CallCount);
        Assert.Null(writer.AccountId);
        Assert.False(writer.WasAuthorized);
    }

    private sealed class FakePermissionChecker(
        bool result) : IPermissionChecker
    {
        public int CallCount { get; private set; }
        public string? LastAccountId { get; private set; }
        public string? LastPermissionId { get; private set; }

        public Task<bool> HasPermissionAsync(
            string accountId,
            string permissionId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastAccountId = accountId;
            LastPermissionId = permissionId;

            return Task.FromResult(result);
        }
    }

    private sealed class FakeAccessLogWriter : IAccessLogWriter
    {
        public int CallCount { get; private set; }
        public string? AccountId { get; private set; }
        public string? RequestPath { get; private set; }
        public bool WasAuthorized { get; private set; }
        public string? ClientIp { get; private set; }

        public Task WriteAsync(
            string? accountId,
            string requestPath,
            bool wasAuthorized,
            string clientIp,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            AccountId = accountId;
            RequestPath = requestPath;
            WasAuthorized = wasAuthorized;
            ClientIp = clientIp;

            return Task.CompletedTask;
        }
    }
}
