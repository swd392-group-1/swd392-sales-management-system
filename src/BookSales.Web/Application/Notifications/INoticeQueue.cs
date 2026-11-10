namespace BookSales.Application.Notifications;

public interface INoticeQueue
{
    ValueTask EnqueueAsync(IntegrationNotice notice, CancellationToken cancellationToken);
    IAsyncEnumerable<IntegrationNotice> ReadAllAsync(CancellationToken cancellationToken);
}
