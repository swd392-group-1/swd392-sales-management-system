using BookSales.Application.Notifications;
using BookSales.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookSales.Tests;

public sealed class NoticeWorkerTests
{
    [Fact]
    public async Task WorkerContinuesAfterOneDeliveryFails()
    {
        var queue = new ChannelNoticeQueue();
        var failing = new IntegrationNotice(Guid.NewGuid(), "fail", DateTimeOffset.UtcNow);
        var succeeding = failing with { Id = Guid.NewGuid(), Message = "succeed" };
        var publisher = new FailOncePublisher(failing.Id);
        using var worker = new NoticeDispatchWorker(queue, publisher, NullLogger<NoticeDispatchWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await queue.EnqueueAsync(failing, CancellationToken.None);
            await queue.EnqueueAsync(succeeding, CancellationToken.None);
            var received = await publisher.Delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(succeeding.Id, received.Id);
        }
        finally
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StopAsync(timeout.Token);
        }
    }

    private sealed class FailOncePublisher(Guid failingId) : INoticePublisher
    {
        public TaskCompletionSource<IntegrationNotice> Delivered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PublishAsync(IntegrationNotice notice, CancellationToken cancellationToken)
        {
            if (notice.Id == failingId) throw new InvalidOperationException("Simulated transport failure.");
            Delivered.TrySetResult(notice);
            return Task.CompletedTask;
        }
    }
}
