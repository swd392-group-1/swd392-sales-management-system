using BookSales.Application.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookSales.Infrastructure.Notifications;

public sealed class NoticeDispatchWorker(
    INoticeQueue queue,
    INoticePublisher publisher,
    ILogger<NoticeDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var notice in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await publisher.PublishAsync(notice, stoppingToken);
                    logger.LogInformation("Dispatched development notice {NoticeId}", notice.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // A failed transport operation must not stop later notices.
                    // No retry/durable-delivery guarantee is implied by this queue.
                    logger.LogError(ex, "Development notice {NoticeId} failed", notice.Id);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown.
        }
    }
}
