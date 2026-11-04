using BookSales.Infrastructure.Notifications;
using Microsoft.AspNetCore.SignalR;
namespace BookSales.Web.Realtime;
public sealed class PriceDispatchWorker(PriceChangeQueue queue, IHubContext<PriceHub> hub,
    ILogger<PriceDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var price in queue.ReadAllAsync(stoppingToken))
        {
            try { await hub.Clients.All.SendAsync("BookPriceChanged", price, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Unable to deliver price change for {BookId}", price.BookId); }
        }
    }
}
