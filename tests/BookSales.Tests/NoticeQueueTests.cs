using BookSales.Application.Notifications;
using BookSales.Infrastructure.Notifications;

namespace BookSales.Tests;

public sealed class NoticeQueueTests
{
    [Fact]
    public async Task CancelledFullQueueWriteDoesNotReplaceAcceptedNotice()
    {
        var queue = new ChannelNoticeQueue(capacity: 1);
        var accepted = new IntegrationNotice(Guid.NewGuid(), "accepted", DateTimeOffset.UtcNow);
        await queue.EnqueueAsync(accepted, CancellationToken.None);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await queue.EnqueueAsync(accepted with { Id = Guid.NewGuid() }, cancelled.Token));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var reader = queue.ReadAllAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(accepted, reader.Current);
    }

    [Fact]
    public async Task ProducerWaitsForCapacityRatherThanDroppingNotices()
    {
        var queue = new ChannelNoticeQueue(capacity: 1);
        var first = new IntegrationNotice(Guid.NewGuid(), "first", DateTimeOffset.UtcNow);
        var second = first with { Id = Guid.NewGuid(), Message = "second" };
        await queue.EnqueueAsync(first, CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var pendingWrite = queue.EnqueueAsync(second, timeout.Token).AsTask();
        Assert.False(pendingWrite.IsCompleted);
        await using var reader = queue.ReadAllAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(first.Id, reader.Current.Id);
        await pendingWrite;
        Assert.True(await reader.MoveNextAsync());
        Assert.Equal(second.Id, reader.Current.Id);
    }
}
