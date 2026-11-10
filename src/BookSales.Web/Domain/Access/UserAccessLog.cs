
namespace BookSales.Domain.Access;

public sealed class UserAccessLog
{
    public long AccessLogId { get; set; }
    public string? AccountId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string RequestPath { get; set; } = string.Empty;
    public bool WasAuthorized { get; set; }
    public string ClientIp { get; set; } = string.Empty;
}
