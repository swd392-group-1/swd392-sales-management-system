using BookSales.Application.Access;
using BookSales.Infrastructure.Access;

namespace BookSales.Tests;

public sealed class AccessGuardFailureTests
{
    [Fact]
    public async Task AuthorizeAsync_AuditFailure_DoesNotReturnAnAllowDecision()
    {
        var guard = new AccessGuard(new AlwaysAllow(), new FailingAudit());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            guard.AuthorizeAsync("ACC-001", "UC12", "/access/permissions", "127.0.0.1"));
    }

    private sealed class AlwaysAllow : IPermissionChecker
    {
        public Task<bool> HasPermissionAsync(string accountId, string permissionId,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class FailingAudit : IAccessLogWriter
    {
        public Task WriteAsync(string? accountId, string requestPath,
            bool wasAuthorized, string clientIp,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Audit writer intentionally unavailable.");
    }
}
