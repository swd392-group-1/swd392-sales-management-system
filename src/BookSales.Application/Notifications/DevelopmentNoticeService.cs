namespace BookSales.Application.Notifications;

public sealed class DevelopmentNoticeService(INoticeQueue queue, TimeProvider clock)
{
    public async Task<IntegrationNotice> SubmitAsync(string message, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        // Transport-probe input limit; this is not a bookstore business rule.
        if (message.Length > 200)
        {
            throw new ArgumentException("Nội dung kiểm tra tối đa 200 ký tự.", nameof(message));
        }

        var notice = new IntegrationNotice(Guid.NewGuid(), message.Trim(), clock.GetUtcNow());
        await queue.EnqueueAsync(notice, cancellationToken);
        return notice; // Accepted by queue does not mean delivered to a client.
    }
}
