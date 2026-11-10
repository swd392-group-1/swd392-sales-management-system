namespace BookSales.Application.Notifications;

public interface INoticePublisher
{
    Task PublishAsync(IntegrationNotice notice, CancellationToken cancellationToken);
}
