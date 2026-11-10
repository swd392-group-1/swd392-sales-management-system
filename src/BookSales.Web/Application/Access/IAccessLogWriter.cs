
namespace BookSales.Application.Access;

public interface IAccessLogWriter
{
    Task WriteAsync(
        string? accountId,
        string requestPath,
        bool wasAuthorized,
        string clientIp,
        CancellationToken cancellationToken = default);
}
