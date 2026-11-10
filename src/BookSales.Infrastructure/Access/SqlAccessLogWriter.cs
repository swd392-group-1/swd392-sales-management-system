
using BookSales.Application.Access;
using BookSales.Domain.Access;
using BookSales.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BookSales.Infrastructure.Access;

public sealed class SqlAccessLogWriter(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock) : IAccessLogWriter
{
    public async Task WriteAsync(
        string? accountId,
        string requestPath,
        bool wasAuthorized,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        if (accountId is not null &&
            (accountId.Length == 0 || accountId.Length > 200))
        {
            throw new ArgumentException(
                "Invalid account ID.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(requestPath) ||
            requestPath.Length > 2048)
        {
            throw new ArgumentException(
                "Invalid request path.", nameof(requestPath));
        }

        if (string.IsNullOrWhiteSpace(clientIp) ||
            clientIp.Length > 45)
        {
            throw new ArgumentException(
                "Invalid client IP.", nameof(clientIp));
        }

        await using var scope = scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BookSalesDbContext>();

        var log = new UserAccessLog
        {
            AccountId = accountId,
            OccurredAtUtc = clock.GetUtcNow().UtcDateTime,
            RequestPath = requestPath,
            WasAuthorized = wasAuthorized,
            ClientIp = clientIp
        };

        db.UserAccessLogs.Add(log);

        await db.SaveChangesAsync(cancellationToken);
    }
}
