namespace BookSales.Application.Notifications;

/// <summary>A development transport probe, not a sale, price change or stock event.</summary>
public sealed record IntegrationNotice(Guid Id, string Message, DateTimeOffset CreatedAtUtc);
