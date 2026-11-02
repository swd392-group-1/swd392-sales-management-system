namespace BookSales.Application.Returns;

public interface IBookReturnRepository
{
    Task<ReturnableOrderDetails?> GetOrderForReturnAsync(
        string orderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BookReturnSummary>> GetReturnHistoryAsync(
        string? orderId,
        CancellationToken cancellationToken);

    Task<BookReturnResult?> GetReturnByIdAsync(
        string returnId,
        CancellationToken cancellationToken);

    Task<BookReturnResult> CreateReturnAsync(
        BookReturnRequest request,
        CancellationToken cancellationToken);
}
