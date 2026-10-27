using BookSales.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace BookSales.Web.Realtime;

public sealed class SignalRNoticePublisher(IHubContext<DevelopmentNoticeHub> hub) : INoticePublisher
{
    public Task PublishAsync(IntegrationNotice notice, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync("NoticeReceived", notice, cancellationToken);
}
