using System.Threading.Channels;
using BookSales.Application.Notifications;
using BookSales.Domain.Interfaces;
namespace BookSales.Infrastructure.Notifications;

public sealed class PriceChangeQueue : IBackgroundTaskQueue
{
    private readonly Channel<BookPriceChangedEvent> channel = Channel.CreateBounded<BookPriceChangedEvent>(
        new BoundedChannelOptions(128) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    public ValueTask EnqueueAsync(object workItem) => workItem is BookPriceChangedEvent price
        ? channel.Writer.WriteAsync(price)
        : throw new ArgumentException("Only BookPriceChangedEvent is supported.");
    public IAsyncEnumerable<BookPriceChangedEvent> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
