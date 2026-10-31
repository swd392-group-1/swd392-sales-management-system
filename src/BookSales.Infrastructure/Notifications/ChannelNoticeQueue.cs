using System.Threading.Channels;
using BookSales.Application.Notifications;

namespace BookSales.Infrastructure.Notifications;

public sealed class ChannelNoticeQueue : INoticeQueue
{
    private readonly Channel<IntegrationNotice> channel;

    public ChannelNoticeQueue(int capacity = 128)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        channel = Channel.CreateBounded<IntegrationNotice>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(IntegrationNotice notice, CancellationToken cancellationToken) =>
        channel.Writer.WriteAsync(notice, cancellationToken);

    public IAsyncEnumerable<IntegrationNotice> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}