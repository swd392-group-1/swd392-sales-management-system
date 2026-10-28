namespace BookSalesManagement.Application.Common;

public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

internal static class IdFactory
{
    public static string New(string prefix, DateTime utcNow)
        => $"{prefix}-{utcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
